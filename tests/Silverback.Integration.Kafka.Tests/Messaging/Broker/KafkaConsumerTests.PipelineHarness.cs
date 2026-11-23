// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    private enum PipelineHarnessSubscriber
    {
        Single,

        Batch,

        // Two batch subscribers: once the first message is added to the batch, the first one fails, while the second one doesn't
        // return until SecondSubscriberRelease is completed, and the endpoint skips the failed batch
        FailingBatch,

        Stream
    }

    // The partitions are caught up synchronously by the tracker, unlike the caught up notification, which is sent by a separate task
    private static CatchUpTracker GetCatchUpTracker(KafkaConsumer consumer) =>
        typeof(KafkaConsumer)
            .GetField("_catchUp", BindingFlags.Instance | BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .GetValue(consumer)
            .ShouldBeOfType<CatchUpTracker>();

    // Like the PollHarness, only the native client is scripted, while the consumer, its consume loop, its channels and the whole
    // consumer pipeline (sequences, transaction handler, publisher) are real. The catch-up mode, the auto recovery and the subscriber
    // can be configured.
    private sealed class PipelineHarness : IAsyncDisposable, IConsumerBehavior, ISilverbackLogger<KafkaConsumer>, ILogger
    {
        private readonly BlockingCollection<Func<ConsumeResult<byte[]?, byte[]?>?>> _polls = [];

        private readonly ConcurrentDictionary<(int Partition, long Offset), TaskCompletionSource<bool>> _blocked = new();

        private readonly ConcurrentDictionary<(int Partition, long Offset), byte> _uncommitted = new();

        private readonly ConcurrentDictionary<(int Partition, long Offset), Action<KafkaOffset>> _endChecks = new();

        private readonly ConcurrentDictionary<(int Partition, long Offset), byte> _heldUntilStreamsAborted = new();

        private readonly ConcurrentDictionary<int, long> _stored = new();

        private readonly ServiceProvider _serviceProvider;

        private IReadOnlyList<TopicPartition> _assignment = [];

        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
        public PipelineHarness(
            CatchUpMode catchUpMode,
            PipelineHarnessSubscriber subscriber = PipelineHarnessSubscriber.Single,
            bool independent = true,
            bool autoRecovery = false)
        {
            Client = Substitute.For<IConfluentConsumerWrapper>();
            Client.Initialized.Returns(new AsyncEvent<BrokerClient>());
            Client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
            Client.Status.Returns(ClientStatus.Initialized);
            Client.Assignment.Returns(_ => _assignment);
            Client.Consume(Arg.Any<TimeSpan>()).Returns(call =>
                _polls.TryTake(out Func<ConsumeResult<byte[]?, byte[]?>?>? poll, call.Arg<TimeSpan>()) ? poll() : null);

            Client.When(client => client.StoreOffset(Arg.Any<TopicPartitionOffset>())).Do(call =>
            {
                TopicPartitionOffset offset = call.Arg<TopicPartitionOffset>();
                _stored[offset.Partition.Value] = offset.Offset.Value;
            });

            Client.When(client => client.Seek(Arg.Any<TopicPartitionOffset>())).Do(call => Seeks.Enqueue(call.Arg<TopicPartitionOffset>()));

            Client.When(client => client.Pause(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
            {
                foreach (TopicPartition partition in call.Arg<IEnumerable<TopicPartition>>())
                {
                    Pauses.Enqueue(partition);
                }
            });

            Client.When(client => client.Resume(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
            {
                foreach (TopicPartition partition in call.Arg<IEnumerable<TopicPartition>>())
                {
                    Resumes.Enqueue(partition);
                }
            });

            Client.DisconnectAsync().Returns(_ =>
            {
                Disconnected.TrySetResult(true);

                return ValueTask.CompletedTask;
            });

            Client.ReconnectAsync().Returns(_ =>
            {
                Reconnected.TrySetResult(true);

                return ValueTask.CompletedTask;
            });

            ServiceCollection services = new();
            SilverbackBuilder silverbackBuilder = services
                .AddLogging(logging => logging.AddProvider(new PipelineHarnessLoggerProvider(this)).SetMinimumLevel(LogLevel.Trace))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka());

            switch (subscriber)
            {
                case PipelineHarnessSubscriber.Single:
                    silverbackBuilder.AddDelegateSubscriber<IInboundEnvelope<string>>(HandleMessage);
                    break;
                case PipelineHarnessSubscriber.FailingBatch:
                    silverbackBuilder
                        .AddDelegateSubscriber<IAsyncEnumerable<IInboundEnvelope<string>>>(HandleFailingSequenceAsync)
                        .AddDelegateSubscriber<IAsyncEnumerable<IInboundEnvelope<string>>>(HandleHoldingSequenceAsync);
                    break;
                default:
                    silverbackBuilder.AddDelegateSubscriber<IAsyncEnumerable<IInboundEnvelope<string>>>(HandleSequenceAsync);
                    break;
            }

            services.AddSingleton<IBrokerBehavior>(this);
            _serviceProvider = services.BuildServiceProvider();

            Consumer = new KafkaConsumer(
                "consumer",
                Client,
                new KafkaConsumerConfiguration
                {
                    GroupId = "tests",
                    CatchUpMode = catchUpMode,
                    EnableAutoRecovery = autoRecovery,
                    EnableAutoCommit = false,
                    CommitOffsetEach = 1,
                    ProcessPartitionsIndependently = independent,
                    MaxDegreeOfParallelism = independent ? 100 : 1,
                    PollingTimeout = TimeSpan.FromMilliseconds(10),
                    Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                    [
                        new KafkaConsumerEndpointConfiguration
                        {
                            Batch = subscriber is PipelineHarnessSubscriber.Batch or PipelineHarnessSubscriber.FailingBatch
                                ? new BatchSettings { Size = 10 }
                                : null,
                            AllowStreaming = subscriber == PipelineHarnessSubscriber.Stream,
                            ErrorPolicy = subscriber == PipelineHarnessSubscriber.FailingBatch
                                ? new SkipMessageErrorPolicy()
                                : new StopConsumerErrorPolicy(),
                            Deserializer = new JsonMessageDeserializer<string>(),
                            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                                [new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                        }
                    ])
                },
                _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Substitute.For<IKafkaOffsetStoreFactory>(),
                _serviceProvider,
                this);
        }

        public static TopicPartition Partition0 { get; } = new("topic", 0);

        public KafkaConsumer Consumer { get; }

        public IConfluentConsumerWrapper Client { get; }

        public ConcurrentQueue<TopicPartitionOffset> Seeks { get; } = new();

        public ConcurrentQueue<TopicPartition> Pauses { get; } = new();

        public ConcurrentQueue<TopicPartition> Resumes { get; } = new();

        public ConcurrentQueue<(int Partition, long Offset)> Handled { get; } = new();

        public ConcurrentQueue<IReadOnlyCollection<MessageHeader>> Headers { get; } = new();

        // The records received by the subscriber
        public ConcurrentQueue<(int Partition, long Offset)> Subscribed { get; } = new();

        public ConcurrentQueue<int> CompletedSequenceSizes { get; } = new();

        public ConcurrentQueue<(LogLevel Level, int EventId, string Message)> Logs { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public TaskCompletionSource<bool> Disconnected { get; } = NewSignal();

        public TaskCompletionSource<bool> Reconnected { get; } = NewSignal();

        // Completed when the streams of the batch are aborted, because its first subscriber failed (see FailingBatch)
        public TaskCompletionSource<bool> StreamsAborted { get; } = NewSignal();

        // Set by the second subscriber of the FailingBatch, if its stream ends because it's aborted rather than completed
        public bool IsHoldingStreamAborted { get; private set; }

        public TaskCompletionSource<bool> SecondSubscriberRelease { get; } = NewSignal();

        public TaskCompletionSource<bool> BatchAddCompleted { get; } = NewSignal();

        public int SortIndex => 0;

        public ILogger InnerLogger => this;

        public static ConsumeResult<byte[]?, byte[]?> Record(int partition, long offset, string? key = null) => new()
        {
            TopicPartitionOffset = new TopicPartitionOffset("topic", partition, offset),
            Message = new Message<byte[]?, byte[]?>
            {
                Key = key == null ? null : Encoding.UTF8.GetBytes(key),
                Value = Encoding.UTF8.GetBytes("\"message\""),
                Timestamp = new Timestamp(new DateTime(2026, 11, 23, 10, 30, 0, DateTimeKind.Utc)),
                Headers = []
            }
        };

        public Task WaitUntilCaughtUpAsync() => Consumer.WaitUntilCaughtUpAsync().WaitAsync(Timeout);

        public async Task StartAsync(params int[] partitions)
        {
            await Consumer.StartAsync().AsTask().WaitAsync(Timeout);
            await PollAsync(() =>
            {
                _assignment = [.. Consumer.OnPartitionsAssigned(
                        [.. partitions.Select(partition => new TopicPartitionOffset("topic", partition, StoredOffset(partition)))])
                    .Select(offset => offset.TopicPartition)];

                return null;
            });
        }

        public Task PollAsync(Func<ConsumeResult<byte[]?, byte[]?>?> poll)
        {
            TaskCompletionSource<bool> done = NewSignal();
            _polls.Add(() =>
            {
                try
                {
                    ConsumeResult<byte[]?, byte[]?>? record = poll();
                    done.TrySetResult(true);

                    return record;
                }
                catch (Exception ex)
                {
                    done.TrySetException(ex);

                    throw;
                }
            });

            return done.Task.WaitAsync(Timeout);
        }

        public async Task DeliverAsync(params (int Partition, long Offset)[] records)
        {
            foreach ((int partition, long offset) in records)
            {
                await PollAsync(() => Record(partition, offset));
            }

            // The next native Consume cannot execute until the previous returned record has been enqueued
            await PollAsync(() => null);
        }

        public async Task DeliverEndOfPartitionAsync(int partition, long offset)
        {
            await PollAsync(() => new ConsumeResult<byte[]?, byte[]?>
            {
                IsPartitionEOF = true,
                TopicPartitionOffset = new TopicPartitionOffset("topic", partition, offset)
            });

            // The end of the partition is tracked before the next native Consume
            await PollAsync(() => null);
        }

        // The record waits in front of the pipeline until the returned signal is completed
        public TaskCompletionSource<bool> Block(int partition, long offset)
        {
            TaskCompletionSource<bool> gate = NewSignal();
            _blocked[(partition, offset)] = gate;

            return gate;
        }

        // The record skips the pipeline, so it's never committed (like a record filtered out in a batch)
        public void SkipCommit(int partition, long offset) => _uncommitted[(partition, offset)] = 0;

        // The record leaves the pipeline once the streams of its batch are aborted (see FailingBatch)
        public void HoldUntilStreamsAborted(int partition, long offset) => _heldUntilStreamsAborted[(partition, offset)] = 0;

        // Invoked once, on the reader thread, while the consumer determines the last committable offset of the partition after
        // reading the specified record: between the check of the end of the partition and the update of the catch-up state
        public void BeforeCatchUpUpdate(int partition, long offset, Action<KafkaOffset> action) => _endChecks[(partition, offset)] = action;

        // The reader starts reading the next record only once the catch-up checks of the end of the partition (and of the last record
        // before it) are completed
        public Task WaitUntilEndOfPartitionIsReadAsync(int partition, long offset) =>
            PollHarness.WaitUntilAsync(() => Logs
                .SkipWhile(log => !log.Message.StartsWith($"Partition EOF reached: topic[{partition}]@{offset}", StringComparison.Ordinal))
                .Skip(1)
                .Any(log => log.Message.StartsWith($"Reading channel topic[{partition}]", StringComparison.Ordinal)));

        public bool IsPartitionCaughtUp(int partition) => GetCatchUpTracker(Consumer).IsPartitionCaughtUp(new TopicPartition("topic", partition));

        public long StoredOffset(int partition) => _stored.GetValueOrDefault(partition);

        public int CountPauses(TopicPartition partition) => Pauses.Count(paused => paused == partition);

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            KafkaOffset offset = (KafkaOffset)context.Envelope.BrokerMessageIdentifier;
            (int Partition, long Offset) key = (offset.TopicPartition.Partition.Value, offset.Offset.Value);
            Headers.Enqueue([.. context.Envelope.Headers]);

            if (_endChecks.TryRemove(key, out Action<KafkaOffset>? endCheck))
                await context.SequenceStore.AddAsync(CreateEndCheckObserver(context, () => endCheck(offset)));

            if (_blocked.TryRemove(key, out TaskCompletionSource<bool>? gate))
                await gate.Task.WaitAsync(Timeout, cancellationToken);

            if (_uncommitted.ContainsKey(key))
            {
                context.Dispose();
                Handled.Enqueue(key);

                return;
            }

            await next(context, cancellationToken);

            if (_heldUntilStreamsAborted.TryRemove(key, out _))
            {
                BatchAddCompleted.TrySetResult(true);
                await StreamsAborted.Task.WaitAsync(Timeout, cancellationToken);
            }

            Handled.Enqueue(key);
        }

        public async ValueTask DisposeAsync()
        {
            SecondSubscriberRelease.TrySetResult(true);

            foreach (TaskCompletionSource<bool> gate in _blocked.Values)
            {
                gate.TrySetResult(true);
            }

            await Consumer.StopAsync().AsTask().WaitAsync(Timeout);
            await Task.Run(Consumer.Dispose).WaitAsync(Timeout);
            _polls.Dispose();
            await _serviceProvider.DisposeAsync();
        }

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string text = formatter(state, exception);

            // Errors first: a test waiting for a log may then check the errors
            if (logLevel >= LogLevel.Error)
                Errors.Enqueue($"{text} {exception}");

            Logs.Enqueue((logLevel, eventId.Id, text));
        }

        // The observer is a pending sequence without identifiers: it doesn't affect the catch-up, but it's enumerated while the last
        // committable offset is determined
        private static ISequence CreateEndCheckObserver(ConsumerPipelineContext context, Action action)
        {
            int invoked = 0;
            ISequence observer = Substitute.For<ISequence>();
            observer.SequenceId.Returns("end-check-observer");
            observer.IsPending.Returns(true);
            observer.ParentSequence.Returns((ISequence?)null);
            observer.Context.Returns(context);
            observer.GetCommitIdentifiers().Returns([]);
            observer.GetRollbackIdentifiers().Returns(_ =>
            {
                if (Interlocked.Exchange(ref invoked, 1) == 0)
                    action();

                return [];
            });

            return observer;
        }

        private void HandleMessage(IInboundEnvelope<string> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            Subscribed.Enqueue((offset.TopicPartition.Partition.Value, offset.Offset.Value));
        }

        private async Task HandleFailingSequenceAsync(IAsyncEnumerable<IInboundEnvelope<string>> sequence)
        {
            await using IAsyncEnumerator<IInboundEnvelope<string>> enumerator = sequence.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();

            // Requesting the next message acknowledges the first one, so that it's added to the batch before the failure
            _ = enumerator.MoveNextAsync().AsTask();
            await BatchAddCompleted.Task.WaitAsync(Timeout);

            throw new InvalidOperationException("Test failure");
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The abort of the stream is expected")]
        private async Task HandleHoldingSequenceAsync(IAsyncEnumerable<IInboundEnvelope<string>> sequence)
        {
            await using IAsyncEnumerator<IInboundEnvelope<string>> enumerator = sequence.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();

            // Nothing else is added to the batch, nor is it completed, while the reader is held: the stream ends only when aborted
            try
            {
                await enumerator.MoveNextAsync();
            }
            catch (Exception)
            {
                IsHoldingStreamAborted = true;
            }

            StreamsAborted.TrySetResult(true);
            await SecondSubscriberRelease.Task.WaitAsync(Timeout);
        }

        private async Task HandleSequenceAsync(IAsyncEnumerable<IInboundEnvelope<string>> sequence)
        {
            int count = 0;

            await foreach (IInboundEnvelope<string> envelope in sequence)
            {
                KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
                Subscribed.Enqueue((offset.TopicPartition.Partition.Value, offset.Offset.Value));
                count++;
            }

            CompletedSequenceSizes.Enqueue(count);
        }
    }

    private sealed class PipelineHarnessLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public PipelineHarnessLoggerProvider(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
