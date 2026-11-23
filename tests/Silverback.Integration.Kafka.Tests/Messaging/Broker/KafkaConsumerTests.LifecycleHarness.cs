// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Serialization;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    private sealed class LifecycleConsumer : KafkaConsumer
    {
        public LifecycleConsumer(
            IConfluentConsumerWrapper client,
            KafkaConsumerConfiguration configuration,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviors,
            IBrokerClientCallbacksInvoker callbacksInvoker,
            IKafkaOffsetStoreFactory offsetStoreFactory,
            ISilverbackLogger<KafkaConsumer> logger)
            : base(
                "consumer",
                client,
                configuration,
                behaviors,
                callbacksInvoker,
                offsetStoreFactory,
                Substitute.For<IServiceProvider>(),
                logger)
        {
        }

        public ValueTask RollbackCoreForTestAsync(IReadOnlyCollection<KafkaOffset> offsets) => RollbackCoreAsync(offsets);

        public void DisposeForTest(bool disposing) => Dispose(disposing);
    }

    // Only the native client, the callbacks and the offset store are scripted: the consumer, its polling loop and its channels are real.
    // The scripted callbacks execute synchronously on the thread calling Consume, as the Confluent client does.
    private sealed class LifecycleHarness : IAsyncDisposable, IConsumerBehavior, ISilverbackLogger<KafkaConsumer>, ILogger
    {
        private readonly BlockingCollection<Func<ConsumeResult<byte[]?, byte[]?>?>> _polls = [];

        private readonly ConcurrentDictionary<TopicPartition, byte> _paused = new();

        private IReadOnlyList<TopicPartition> _assignment = [];

        private IReadOnlyCollection<KafkaOffset> _storedOffsets = [];

        private bool _isConsumerDisposed;

        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
        public LifecycleHarness(Func<KafkaConsumerConfiguration, KafkaConsumerConfiguration>? configure = null)
        {
            Client = Substitute.For<IConfluentConsumerWrapper>();
            Client.Initialized.Returns(new AsyncEvent<BrokerClient>());
            Client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
            Client.Status.Returns(_ => Status);
            Client.Assignment.Returns(_ => _assignment);
            Client.Consume(Arg.Any<TimeSpan>()).Returns(call =>
                _polls.TryTake(out Func<ConsumeResult<byte[]?, byte[]?>?>? poll, call.Arg<TimeSpan>()) ? poll() : null);

            Client.When(client => client.StoreOffset(Arg.Any<TopicPartitionOffset>())).Do(call =>
            {
                TopicPartitionOffset offset = call.Arg<TopicPartitionOffset>();
                BeforeStore?.Invoke(offset);
                Stored.Enqueue(offset);
            });

            Client.When(client => client.Seek(Arg.Any<TopicPartitionOffset>())).Do(call => Seeks.Enqueue(call.Arg<TopicPartitionOffset>()));

            Client.When(client => client.Pause(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
            {
                foreach (TopicPartition partition in call.Arg<IEnumerable<TopicPartition>>())
                {
                    _paused[partition] = 0;
                    Pauses.Enqueue(partition);
                }
            });

            Client.When(client => client.Resume(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
            {
                foreach (TopicPartition partition in call.Arg<IEnumerable<TopicPartition>>())
                {
                    _paused.TryRemove(partition, out _);
                    Resumes.Enqueue(partition);
                }
            });

            OffsetStore = Substitute.For<IKafkaOffsetStore>();
            OffsetStore.GetStoredOffsets(Arg.Any<string>()).Returns(_ => _storedOffsets);

            OffsetStoreFactory = Substitute.For<IKafkaOffsetStoreFactory>();
            OffsetStoreFactory.GetStore(Arg.Any<KafkaOffsetStoreSettings>(), Arg.Any<IServiceProvider>()).Returns(_ =>
            {
                BeforeGetStore?.Invoke();

                return OffsetStore;
            });

            CallbacksInvoker.InvokeAsync(
                    Arg.Any<Func<IKafkaConsumerCaughtUpCallback, Task>>(),
                    Arg.Any<IServiceProvider?>(),
                    Arg.Any<bool>())
                .Returns(call => new ValueTask(call.Arg<Func<IKafkaConsumerCaughtUpCallback, Task>>().Invoke(CaughtUpCallback)));

            IBrokerBehaviorsProvider<IConsumerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
            behaviors.GetBehaviorsList().Returns([this]);

            KafkaConsumerConfiguration configuration = new()
            {
                GroupId = "tests",
                EnableAutoRecovery = false,
                EnableAutoCommit = false,
                CommitOffsetEach = 1,
                ProcessPartitionsIndependently = true,
                BackpressureLimit = 8,
                MaxDegreeOfParallelism = 100,
                PollingTimeout = TimeSpan.FromMilliseconds(10),
                Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                [
                    new KafkaConsumerEndpointConfiguration
                    {
                        Deserializer = new JsonMessageDeserializer<string>(),
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>([new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                    }
                ])
            };

            Consumer = new LifecycleConsumer(
                Client,
                configure?.Invoke(configuration) ?? configuration,
                behaviors,
                CallbacksInvoker,
                OffsetStoreFactory,
                this);
        }

        public LifecycleConsumer Consumer { get; }

        public IConfluentConsumerWrapper Client { get; }

        public IBrokerClientCallbacksInvoker CallbacksInvoker { get; } = Substitute.For<IBrokerClientCallbacksInvoker>();

        public IKafkaConsumerCaughtUpCallback CaughtUpCallback { get; } = Substitute.For<IKafkaConsumerCaughtUpCallback>();

        public IKafkaOffsetStoreFactory OffsetStoreFactory { get; }

        public IKafkaOffsetStore OffsetStore { get; }

        public ClientStatus Status { get; set; } = ClientStatus.Initialized;

        // Invoked for each handled record instead of committing it
        public Func<KafkaOffset, CancellationToken, Task>? OnMessage { get; set; }

        public Action<TopicPartitionOffset>? BeforeStore { get; set; }

        public Action? BeforeGetStore { get; set; }

        public ConcurrentQueue<KafkaOffset> Handled { get; } = new();

        public ConcurrentQueue<KafkaOffset> HandlerCommits { get; } = new();

        public ConcurrentQueue<TopicPartitionOffset> Stored { get; } = new();

        public ConcurrentQueue<TopicPartitionOffset> Seeks { get; } = new();

        public ConcurrentQueue<TopicPartition> Pauses { get; } = new();

        public ConcurrentQueue<TopicPartition> Resumes { get; } = new();

        // The native pause state, which the scripted rebalances don't reset
        public IReadOnlyCollection<TopicPartition> Paused => [.. _paused.Keys];

        public ConcurrentQueue<string> Logs { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public int SortIndex => 0;

        public ILogger InnerLogger => this;

        public static async Task WaitUntilAsync(Func<bool> condition)
        {
            using CancellationTokenSource timeout = new(Timeout);

            while (!condition())
            {
                await Task.Delay(1, timeout.Token);
            }
        }

        public static TopicPartition ToTopicPartition(int partition) => new("topic", partition);

        public void SetStoredOffsets(params KafkaOffset[] offsets) => _storedOffsets = offsets;

        public Task StartAsync(params int[] partitions) =>
            StartAsync([.. partitions.Select(partition => new TopicPartitionOffset("topic", partition, 0))]);

        public async Task StartAsync(IReadOnlyCollection<TopicPartitionOffset> offsets)
        {
            await Consumer.StartAsync();
            await PollAsync(() =>
            {
                AssignOffsets(offsets);

                return null;
            });
        }

        public void AssignOffsets(IReadOnlyCollection<TopicPartitionOffset> offsets)
        {
            IReadOnlyCollection<TopicPartitionOffset> accepted = Consumer.OnPartitionsAssigned(offsets);
            _assignment = [.. accepted.Select(offset => offset.TopicPartition)];
        }

        public void Revoke(params int[] partitions)
        {
            Consumer.OnPartitionsRevoked([.. partitions.Select(partition => new TopicPartitionOffset("topic", partition, Offset.Unset))]);
            _assignment = [.. _assignment.Where(partition => !partitions.Contains(partition.Partition.Value))];
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
                await PollAsync(() => new ConsumeResult<byte[]?, byte[]?>
                {
                    TopicPartitionOffset = new TopicPartitionOffset("topic", partition, offset),
                    Message = new Message<byte[]?, byte[]?>
                    {
                        Value = [1],
                        Headers = []
                    }
                });
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

            // The next native Consume cannot execute until the previous returned result has been processed
            await PollAsync(() => null);
        }

        public PartitionChannel GetChannel(int partition)
        {
            ConsumerChannelsManager channelsManager = typeof(KafkaConsumer)
                .GetField("_channelsManager", BindingFlags.Instance | BindingFlags.NonPublic)
                .ShouldNotBeNull()
                .GetValue(Consumer)
                .ShouldBeOfType<ConsumerChannelsManager>();

            return channelsManager.GetChannel(ToTopicPartition(partition)).ShouldNotBeNull();
        }

        public bool IsPartitionCaughtUp(int partition) => GetCatchUpTracker(Consumer).IsPartitionCaughtUp(ToTopicPartition(partition));

        public void DisposeConsumer(bool disposing = true)
        {
            Consumer.DisposeForTest(disposing);
            _isConsumerDisposed |= disposing;
        }

        public ValueTask RaiseClientInitializedAsync() =>
            Client.Initialized.InvokeAsync(Substitute.For<BrokerClient>("client", Substitute.For<ISilverbackLogger>()));

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            using (context)
            {
                KafkaOffset offset = (KafkaOffset)context.Envelope.BrokerMessageIdentifier;
                Handled.Enqueue(offset);

                if (OnMessage != null)
                {
                    await OnMessage(offset, cancellationToken);

                    return;
                }

                await Consumer.CommitAsync(offset);
                HandlerCommits.Enqueue(offset);
            }
        }

        public async ValueTask DisposeAsync()
        {
            Status = ClientStatus.Initialized;

            if (!_isConsumerDisposed)
            {
                await Consumer.StopAsync().AsTask().WaitAsync(Timeout);
                await Task.Run(Consumer.Dispose).WaitAsync(Timeout);
            }

            _polls.Dispose();
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
                Errors.Enqueue(text);

            Logs.Enqueue(text);
        }

        public bool HasLog(string prefix) => Logs.Any(log => log.StartsWith(prefix, StringComparison.Ordinal));
    }
}
