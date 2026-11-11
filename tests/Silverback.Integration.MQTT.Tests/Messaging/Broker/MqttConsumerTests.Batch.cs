// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker;

public partial class MqttConsumerTests
{
    // Not added to the batch, like a message that no subscriber stream accepts: the reader acknowledges it as soon as it's processed
    private const int UntrackedMessage = 99;

    private static readonly TimeSpan BatchTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public void Constructor_ShouldLogBatchProcessingEnabled_WhenBatchesAreAcknowledged(MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        LoggerSubstitute<MqttConsumer> loggerSubstitute = new(LogLevel.Trace);
        SilverbackLogger<MqttConsumer> logger = new(new MappedLevelsLogger<MqttConsumer>([], loggerSubstitute));
        using MqttClientWrapper client = CreateBatchLoggingClient(logger);

        using MqttConsumer consumer = CreateBatchLoggingConsumer(client, logger, qualityOfServiceLevel, new BatchSettings { Size = 10 });

        const string expectedMessage =
            "Batch processing enabled: the broker must allow at least 30 unacknowledged messages " +
            "(batch size 10, max degree of parallelism 3) | ConsumerName: batch-log-test";
        loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 4014);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, 10)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, null)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, 1)]
    public void Constructor_ShouldNotLogBatchProcessingEnabled_WhenBatchesAreNotAcknowledged(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        int? batchSize)
    {
        LoggerSubstitute<MqttConsumer> loggerSubstitute = new(LogLevel.Trace);
        SilverbackLogger<MqttConsumer> logger = new(new MappedLevelsLogger<MqttConsumer>([], loggerSubstitute));
        using MqttClientWrapper client = CreateBatchLoggingClient(logger);
        BatchSettings? batch = batchSize == null ? null : new BatchSettings { Size = batchSize.Value };

        using MqttConsumer consumer = CreateBatchLoggingConsumer(client, logger, qualityOfServiceLevel, batch);

        loggerSubstitute.DidNotReceive(LogLevel.Information, null, null, 4014);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task CommitAsync_ShouldAcknowledgeEachDeliveryOnceInReceivedOrder_WhenCommittingBatch(
        MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        await using BatchConsumerHarness harness = new(qualityOfServiceLevel, 4);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0, 1, 2, 3);
        IReadOnlyList<IBrokerMessageIdentifier> firstBatch = await harness.Behavior.ReadBatchAsync();
        await harness.DeliverAndWaitUntilAcknowledgedAsync(UntrackedMessage);

        // The reader moved past the batch, which is acknowledged only when committed
        harness.Acknowledged.ShouldBe([UntrackedMessage]);

        await harness.Consumer.CommitAsync([firstBatch[2], firstBatch[0], firstBatch[3], firstBatch[1]]);
        await harness.Consumer.CommitAsync(firstBatch);

        harness.Acknowledged.ShouldBe([UntrackedMessage, 0, 1, 2, 3]);

        await harness.DeliverAsync(4, 5, 6, 7);
        IReadOnlyList<IBrokerMessageIdentifier> secondBatch = await harness.Behavior.ReadBatchAsync();
        await harness.Consumer.CommitAsync([.. secondBatch.Reverse()]);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(BatchTimeout);

        harness.Behavior.Processed.ShouldBe([0, 1, 2, 3, UntrackedMessage, 4, 5, 6, 7]);
        harness.Acknowledged.ShouldBe([UntrackedMessage, 0, 1, 2, 3, 4, 5, 6, 7]);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    public async Task CommitAsync_ShouldDisconnect_WhenQoS2AcknowledgmentFailsAfterReaderMovedPastBatch(int failingMessage, bool timeout)
    {
        await using BatchConsumerHarness harness = new(MqttQualityOfServiceLevel.ExactlyOnce, 3);
        Exception failure = timeout
            ? new OperationCanceledException("Acknowledgment timed out.")
            : new InvalidOperationException("Acknowledgment failed while still connected.");
        harness.FailAcknowledgment(failingMessage, failure);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0, 1, 2);
        IReadOnlyList<IBrokerMessageIdentifier> batch = await harness.Behavior.ReadBatchAsync();

        // The reader moved past the batch and doesn't wait for its messages, so it can't observe the acknowledgment failure
        await harness.DeliverAndWaitUntilAcknowledgedAsync(UntrackedMessage);

        // Caught to compare the instance, since a task faulted by an OperationCanceledException is canceled instead
        Exception? exception = null;

        try
        {
            await harness.Consumer.CommitAsync(batch);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await AsyncTestingUtil.WaitAsync(() => harness.Client.Status == ClientStatus.Disconnected, BatchTimeout);

        exception.ShouldBeSameAs(failure);
        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
        harness.Acknowledged.ShouldBe([UntrackedMessage, .. Enumerable.Range(0, failingMessage)]);
    }

    [Fact]
    public async Task CommitAsync_ShouldDisconnect_WhenQoS2AcknowledgmentTimesOutWhileReaderAwaitsBatch()
    {
        await using BatchConsumerHarness harness = new(MqttQualityOfServiceLevel.ExactlyOnce, 3);
        harness.FailAcknowledgment(1, new OperationCanceledException("Acknowledgment timed out."));

        // Committed while the reader awaits the batch it completed: the processing fails with the acknowledgment timeout, as when the
        // error policy (e.g. Stop) doesn't handle the commit failure
        harness.Behavior.OnBatchCompleted = async (consumer, batch) => await consumer.CommitAsync(batch);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0, 1, 2);

        // The reader observes the acknowledgment failure instead of taking it for a cancellation, which would leave the deliveries pending
        await AsyncTestingUtil.WaitAsync(() => harness.Client.Status == ClientStatus.Disconnected, BatchTimeout);

        harness.Client.Status.ShouldBe(ClientStatus.Disconnected);
        harness.Acknowledged.ShouldBe([0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RollbackAsync_ShouldReplayDeliveriesInOrderAndAcknowledgeEachOnce_WhenBatchIsRolledBack(
        bool rollbackWhileProcessingLastMessage)
    {
        await using BatchConsumerHarness harness = new(MqttQualityOfServiceLevel.AtLeastOnce, 3);
        int[] untrackedMessages = rollbackWhileProcessingLastMessage ? [] : [UntrackedMessage];
        int completedBatches = 0;

        // Rolled back by the processing of the message completing the batch, as the error policies do when the batch processing fails
        if (rollbackWhileProcessingLastMessage)
        {
            harness.Behavior.OnBatchCompleted = async (consumer, batch) =>
            {
                if (Interlocked.Increment(ref completedBatches) == 1)
                    await consumer.RollbackAsync(batch);
            };
        }

        await harness.ConnectAsync();
        await harness.DeliverAsync(0, 1, 2);
        IReadOnlyList<IBrokerMessageIdentifier> firstBatch = await harness.Behavior.ReadBatchAsync();

        // Rolled back after the reader moved past the batch
        if (!rollbackWhileProcessingLastMessage)
        {
            await harness.DeliverAndWaitUntilAcknowledgedAsync(UntrackedMessage);
            await harness.Consumer.RollbackAsync(firstBatch);
        }

        IReadOnlyList<IBrokerMessageIdentifier> replayedBatch = await harness.Behavior.ReadBatchAsync();

        replayedBatch.ShouldBe(firstBatch);
        harness.Behavior.Processed.ShouldBe([0, 1, 2, .. untrackedMessages, 0, 1, 2]);
        harness.Acknowledged.ShouldBe(untrackedMessages);

        await harness.Consumer.CommitAsync(replayedBatch);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(BatchTimeout);

        harness.Behavior.Processed.ShouldBe([0, 1, 2, .. untrackedMessages, 0, 1, 2]);
        harness.Acknowledged.ShouldBe([.. untrackedMessages, 0, 1, 2]);
    }

    [Fact]
    public async Task RollbackAsync_ShouldNotRestartReader_WhenConsumerIsStoppedWhileRestartIsPending()
    {
        // The restart and the stop race when the stopped reader exits: repeat to cover both orders
        for (int i = 0; i < 20; i++)
        {
            await RollbackAndStopWhileRestartIsPendingAsync();
        }
    }

    private static async Task RollbackAndStopWhileRestartIsPendingAsync()
    {
        await using BatchConsumerHarness harness = new(MqttQualityOfServiceLevel.AtLeastOnce, 3);
        TaskCompletionSource<bool> secondMessageProcessing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseSecondMessage = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int secondMessageProcessingCount = 0;

        harness.Behavior.OnProcessing = async number =>
        {
            if (number != 1 || Interlocked.Increment(ref secondMessageProcessingCount) > 1)
                return;

            secondMessageProcessing.TrySetResult(true);
            await releaseSecondMessage.Task.WaitAsync(BatchTimeout);
        };

        await harness.ConnectAsync();
        await harness.DeliverAsync(0, 1);
        await secondMessageProcessing.Task.WaitAsync(BatchTimeout);
        ConsumerChannel channel = harness.GetChannel();

        // The first message is tracked by the pending batch: its rollback requests the restart, which stops the reader and waits
        // for the processing of the second message
        await harness.Consumer.RollbackAsync([harness.Behavior.Identifiers[0]]);
        await AsyncTestingUtil.WaitAsync(() => channel.IsReadCanceled, BatchTimeout);
        channel.IsReadCanceled.ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);

        // The stop takes over the pending restart request, while the processing of the second message is still blocked
        Task stopTask = harness.Consumer.StopAsync().AsTask();
        await AsyncTestingUtil.WaitAsync(() => channel.GetRestartRequestsCount() == 0, BatchTimeout);
        channel.GetRestartRequestsCount().ShouldBe(0);

        releaseSecondMessage.TrySetResult(true);
        await stopTask.WaitAsync(BatchTimeout);

        // The restart overtaken by the stop doesn't start a new reader
        await AsyncTestingUtil.WaitAsync(() => !channel.IsReadCanceled, TimeSpan.FromMilliseconds(200));
        channel.IsReadCanceled.ShouldBeTrue();
        harness.Behavior.Processed.ShouldBe([0, 1]);

        // Both messages are replayed in order once started again, and the restart overtaken by the stop didn't touch the requests
        await harness.Consumer.StartAsync().AsTask().WaitAsync(BatchTimeout);
        await AsyncTestingUtil.WaitAsync(() => harness.Behavior.Processed.Count >= 4, BatchTimeout);

        harness.Behavior.Processed.ShouldBe([0, 1, 0, 1]);
        channel.GetRestartRequestsCount().ShouldBe(0);
        channel.IsRestartPending.ShouldBeFalse();
    }

    private static MqttClientWrapper CreateBatchLoggingClient(ISilverbackLogger logger) =>
        new(
            "batch-log-client",
            Substitute.For<IMqttClient>(),
            new MqttClientConfiguration
            {
                ClientId = "batch-log-client",
                Channel = new MqttClientTcpConfiguration { RemoteEndpoint = new DnsEndPoint("test-broker", 1234) }
            },
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            logger);

    private static MqttConsumer CreateBatchLoggingConsumer(
        MqttClientWrapper client,
        ISilverbackLogger<MqttConsumer> logger,
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        BatchSettings? batch)
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISequenceStore)).Returns(Substitute.For<ISequenceStore>());

        return new MqttConsumer(
            "batch-log-test",
            client,
            new MqttClientConfiguration
            {
                MaxDegreeOfParallelism = 3,
                ConsumerEndpoints = new ValueReadOnlyCollection<MqttConsumerEndpointConfiguration>(
                [
                    new MqttConsumerEndpointConfiguration
                    {
                        Topics = new ValueReadOnlyCollection<string>(["topic1"]),
                        QualityOfServiceLevel = qualityOfServiceLevel,
                        Batch = batch
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            serviceProvider,
            logger);
    }

    private sealed class BatchConsumerHarness : IAsyncDisposable
    {
        private readonly IMqttClient _nativeClient = Substitute.For<IMqttClient>();

        private readonly MqttQualityOfServiceLevel _qualityOfServiceLevel;

        private readonly TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private Func<MqttApplicationMessageReceivedEventArgs, Task> _messageReceived = null!;

        private (int Number, Exception Exception)? _acknowledgmentFailure;

        private bool _isConnected;

        public BatchConsumerHarness(MqttQualityOfServiceLevel qualityOfServiceLevel, int batchSize)
        {
            _qualityOfServiceLevel = qualityOfServiceLevel;
            Behavior = new BatchTrackingBehavior(batchSize);

            _nativeClient.IsConnected.Returns(_ => _isConnected);

            _nativeClient.When(client => client.ApplicationMessageReceivedAsync += Arg.Any<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
                .Do(call => _messageReceived = call.Arg<Func<MqttApplicationMessageReceivedEventArgs, Task>>());

            _nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                _isConnected = true;

                return new MqttClientConnectResult();
            });

            _nativeClient.DisconnectAsync(Arg.Any<MqttClientDisconnectOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                _isConnected = false;

                return Task.CompletedTask;
            });

            _nativeClient.SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>())
                .Returns(new MqttClientSubscribeResult(0, [], string.Empty, []));

            MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
                .ConnectViaTcp("batch-test-broker")
                .WithClientId("batch-test")
                .Consume(endpoint => endpoint.ConsumeFrom("topic"))
                .Build();
            Client = new MqttClientWrapper(
                "batch-test",
                _nativeClient,
                configuration,
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Substitute.For<ISilverbackLogger>());

            IBrokerBehaviorsProvider<IConsumerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
            behaviors.GetBehaviorsList().Returns([Behavior]);
            Consumer = new MqttConsumer(
                "batch-test",
                Client,
                configuration,
                behaviors,
                Substitute.For<IServiceProvider>(),
                Substitute.For<ISilverbackLogger<MqttConsumer>>());
            Behavior.Consumers = new ConsumerCollection { Consumer };

            // Added after the consumer, which updates its status in its own handler
            Client.Subscribed.AddHandler(_ =>
            {
                _subscribed.TrySetResult(true);

                return ValueTask.CompletedTask;
            });
        }

        public MqttClientWrapper Client { get; }

        public MqttConsumer Consumer { get; }

        public BatchTrackingBehavior Behavior { get; }

        public ConcurrentQueue<int> Acknowledged { get; } = new();

        public void FailAcknowledgment(int number, Exception exception) => _acknowledgmentFailure = (number, exception);

        // The channels aren't exposed by the consumer
        public ConsumerChannel GetChannel()
        {
            object channelsManager = typeof(MqttConsumer)
                .GetField("_channelsManager", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Consumer)!;

            return ((ConsumerChannel[])typeof(ConsumerChannelsManager)
                    .GetField("_channels", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(channelsManager)!)
                .Single();
        }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync().AsTask().WaitAsync(BatchTimeout);
            await _subscribed.Task.WaitAsync(BatchTimeout);
        }

        public async Task DeliverAsync(params int[] numbers)
        {
            foreach (int number in numbers)
            {
                await DeliverCoreAsync(number);
            }
        }

        public async Task DeliverAndWaitUntilAcknowledgedAsync(int number)
        {
            await DeliverCoreAsync(number);
            await AsyncTestingUtil.WaitAsync(() => Acknowledged.Contains(number), BatchTimeout);

            Acknowledged.ShouldContain(number);
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisconnectAsync().AsTask().WaitAsync(BatchTimeout);
            Consumer.Dispose();
            await Client.DisposeAsync();
            _nativeClient.Dispose();
        }

        private Task DeliverCoreAsync(int number)
        {
            MqttApplicationMessageReceivedEventArgs eventArgs = new(
                "batch-test",
                new MqttApplicationMessage
                {
                    Topic = "topic",
                    Payload = new ReadOnlySequence<byte>([checked((byte)number)]),
                    QualityOfServiceLevel = _qualityOfServiceLevel
                },
                new MqttPublishPacket { QualityOfServiceLevel = _qualityOfServiceLevel },
                (_, _) =>
                {
                    if (_acknowledgmentFailure is { } failure && failure.Number == number)
                        throw failure.Exception;

                    Acknowledged.Enqueue(number);

                    return Task.CompletedTask;
                });

            return _messageReceived(eventArgs).WaitAsync(BatchTimeout);
        }
    }

    // Adds the messages to batches, tracking their identifiers like a sequence does, and hands the complete batches over to be committed
    // or rolled back
    private sealed class BatchTrackingBehavior(int batchSize) : IConsumerBehavior
    {
        private readonly System.Threading.Lock _lock = new();

        private readonly List<IBrokerMessageIdentifier> _pendingBatch = [];

        private readonly Channel<IReadOnlyList<IBrokerMessageIdentifier>> _completedBatches =
            Channel.CreateUnbounded<IReadOnlyList<IBrokerMessageIdentifier>>();

        private MqttMessageIdentifiersTracker? _tracker;

        public int SortIndex => 0;

        public IConsumerCollection Consumers { get; set; } = null!;

        public Func<IConsumer, IReadOnlyList<IBrokerMessageIdentifier>, Task>? OnBatchCompleted { get; set; }

        // Awaited before the message is added to the batch
        public Func<int, Task>? OnProcessing { get; set; }

        public ConcurrentQueue<int> Processed { get; } = new();

        // The identifier of each message, set when it's first added to a batch
        public ConcurrentDictionary<int, IBrokerMessageIdentifier> Identifiers { get; } = new();

        public Task<IReadOnlyList<IBrokerMessageIdentifier>> ReadBatchAsync() =>
            _completedBatches.Reader.ReadAsync().AsTask().WaitAsync(BatchTimeout);

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            using (context)
            {
                int number = context.Envelope.RawMessage!.ReadByte();

                if (OnProcessing != null)
                    await OnProcessing(number);

                IReadOnlyList<IBrokerMessageIdentifier>? completedBatch = AddToBatch(number, context.Envelope.BrokerMessageIdentifier);

                if (completedBatch == null)
                    return;

                if (OnBatchCompleted != null)
                    await OnBatchCompleted(context.Consumer, completedBatch);

                _completedBatches.Writer.TryWrite(completedBatch);
            }
        }

        private IBrokerMessageIdentifier[]? AddToBatch(int number, IBrokerMessageIdentifier identifier)
        {
            lock (_lock)
            {
                Processed.Enqueue(number);

                if (number == UntrackedMessage)
                    return null;

                Identifiers.TryAdd(number, identifier);

                // Each batch has its own tracker, like each sequence
                _tracker ??= new MqttMessageIdentifiersTracker(Consumers);
                _tracker.TrackIdentifier(identifier);
                _pendingBatch.Add(identifier);

                if (_pendingBatch.Count < batchSize)
                    return null;

                IBrokerMessageIdentifier[] completedBatch = [.. _pendingBatch];
                _pendingBatch.Clear();
                _tracker = null;

                return completedBatch;
            }
        }
    }
}
