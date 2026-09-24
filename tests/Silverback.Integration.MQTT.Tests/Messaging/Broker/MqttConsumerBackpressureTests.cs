// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker;

public class MqttConsumerBackpressureTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(1, null, 2)]
    [InlineData(3, null, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(1, 4, 4)]
    [InlineData(4, 1, 1)]
    [InlineData(4, 4, 4)]
    [InlineData(4, 7, 7)]
    public async Task Consume_ShouldApplyBackpressurePerChannel_BeforeAndAfterReconnect(
        int degreeOfParallelism,
        int? configuredLimit,
        int expectedCapacity)
    {
        await using ConsumerHarness harness = new(degreeOfParallelism, configuredLimit);

        for (int connection = 0; connection < 2; connection++)
        {
            int messageCount = (degreeOfParallelism * (expectedCapacity + 1)) + 1;
            harness.Behavior.Reset(messageCount);
            harness.Acknowledged.Clear();
            await harness.ConnectAsync();
            await FillBuffersAsync(harness, degreeOfParallelism, expectedCapacity);

            Task writing = harness.DeliverAsync(messageCount - 1);

            writing.IsCompleted.ShouldBeFalse();
            harness.Acknowledged.ShouldBeEmpty();
            harness.Behavior.Processed.ShouldBeEmpty();

            // Releasing only the target channel must be enough to admit its pending write
            harness.Behavior.Release(0);
            await writing.WaitAsync(Timeout);
            harness.Behavior.ReleaseAll();
            await harness.Behavior.AllProcessed.Task.WaitAsync(Timeout);
            await harness.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);

            harness.Behavior.Processed.Order().ShouldBe(Enumerable.Range(0, messageCount));
            harness.Acknowledged.Order().ShouldBe(Enumerable.Range(0, messageCount));

            for (int channel = 0; channel < degreeOfParallelism; channel++)
            {
                int channelIndex = channel;
                harness.Behavior.Processed.Where(number => number % degreeOfParallelism == channelIndex)
                    .ShouldBe(Enumerable.Range(0, messageCount).Where(number => number % degreeOfParallelism == channelIndex));
            }
        }
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 1)]
    [InlineData(3, 4)]
    public async Task DisconnectAsync_ShouldReleaseWriterAndDiscardBufferedDeliveries_WhenConfiguredBuffersAreFull(
        int degreeOfParallelism,
        int backpressureLimit)
    {
        await using ConsumerHarness harness = new(degreeOfParallelism, backpressureLimit);
        harness.Behavior.Reset(degreeOfParallelism);
        await harness.ConnectAsync();
        await FillBuffersAsync(harness, degreeOfParallelism, backpressureLimit);

        Task writing = harness.DeliverAsync(degreeOfParallelism * (backpressureLimit + 1));
        writing.IsCompleted.ShouldBeFalse();
        harness.PendingReceive = writing;

        Task disconnecting = harness.Client.DisconnectAsync().AsTask();
        await writing.WaitAsync(Timeout);

        disconnecting.IsCompleted.ShouldBeFalse();
        harness.Received.Last().ProcessingFailed.ShouldBeTrue();
        harness.Received.Last().AutoAcknowledge.ShouldBeFalse();
        harness.Acknowledged.ShouldBeEmpty();

        harness.Behavior.ReleaseAll();
        await disconnecting.WaitAsync(Timeout);

        harness.Behavior.Processed.Order().ShouldBe(Enumerable.Range(0, degreeOfParallelism));
        harness.Acknowledged.Order().ShouldBe(Enumerable.Range(0, degreeOfParallelism));
    }

    private static async Task FillBuffersAsync(ConsumerHarness harness, int degreeOfParallelism, int capacity)
    {
        for (int number = 0; number < degreeOfParallelism; number++)
        {
            await harness.DeliverAsync(number).WaitAsync(Timeout);
        }

        await harness.Behavior.AllStarted.Task.WaitAsync(Timeout);

        for (int number = degreeOfParallelism; number < degreeOfParallelism * (capacity + 1); number++)
        {
            await harness.DeliverAsync(number).WaitAsync(Timeout);
        }
    }

    private sealed class ConsumerHarness : IAsyncDisposable
    {
        private readonly IMqttClient _nativeClient = Substitute.For<IMqttClient>();

        private readonly MqttConsumer _consumer;

        private Func<MqttApplicationMessageReceivedEventArgs, Task> _messageReceived = null!;

        private TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _isConnected;

        public ConsumerHarness(int degreeOfParallelism, int? backpressureLimit)
        {
            Behavior = new BlockingBehavior(degreeOfParallelism);
            _nativeClient.IsConnected.Returns(_ => Volatile.Read(ref _isConnected));

            _nativeClient.When(client => client.ApplicationMessageReceivedAsync += Arg.Any<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
                .Do(call => _messageReceived = call.Arg<Func<MqttApplicationMessageReceivedEventArgs, Task>>());

            _nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                Volatile.Write(ref _isConnected, true);

                return new MqttClientConnectResult();
            });

            _nativeClient.DisconnectAsync(Arg.Any<MqttClientDisconnectOptions>(), Arg.Any<CancellationToken>()).Returns(async _ =>
            {
                await PendingReceive;
                Volatile.Write(ref _isConnected, false);
            });

            MqttClientConfigurationBuilder builder = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
                .ConnectViaTcp("backpressure-test-broker")
                .EnableParallelProcessing(degreeOfParallelism)
                .Consume(endpoint => endpoint.ConsumeFrom("topic"));

            if (backpressureLimit.HasValue)
                builder.LimitBackpressure(backpressureLimit.Value);

            MqttClientConfiguration configuration = builder.Build();
            Client = new MqttClientWrapper(
                "backpressure-test",
                _nativeClient,
                configuration,
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Substitute.For<ISilverbackLogger>());
            Client.Subscribed.AddHandler(_ =>
            {
                _subscribed.TrySetResult(true);

                return ValueTask.CompletedTask;
            });

            IBrokerBehaviorsProvider<IConsumerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
            behaviors.GetBehaviorsList().Returns([Behavior]);
            _consumer = new MqttConsumer(
                "backpressure-test",
                Client,
                configuration,
                behaviors,
                Substitute.For<IServiceProvider>(),
                Substitute.For<ISilverbackLogger<MqttConsumer>>());
        }

        public MqttClientWrapper Client { get; }

        public BlockingBehavior Behavior { get; }

        public ConcurrentQueue<int> Acknowledged { get; } = new();

        public ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> Received { get; } = new();

        public Task PendingReceive { get; set; } = Task.CompletedTask;

        public async Task ConnectAsync()
        {
            _subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Client.ConnectAsync().AsTask().WaitAsync(Timeout);
            await _subscribed.Task.WaitAsync(Timeout);
        }

        public Task DeliverAsync(int number)
        {
            MqttApplicationMessageReceivedEventArgs eventArgs = new(
                "backpressure-test",
                new MqttApplicationMessage
                {
                    Topic = "topic",
                    Payload = new ReadOnlySequence<byte>([checked((byte)number)]),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
                },
                new MqttPublishPacket { QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce },
                (_, _) =>
                {
                    Acknowledged.Enqueue(number);

                    return Task.CompletedTask;
                });
            Received.Enqueue(eventArgs);

            return _messageReceived(eventArgs);
        }

        public async ValueTask DisposeAsync()
        {
            Behavior.ReleaseAll();
            await Client.DisconnectAsync().AsTask().WaitAsync(Timeout);
            _consumer.Dispose();
            await Client.DisposeAsync();
            _nativeClient.Dispose();
        }
    }

    private sealed class BlockingBehavior(int degreeOfParallelism) : IConsumerBehavior
    {
        private TaskCompletionSource<bool>[] _release = [];

        private int _started;

        private int _expectedMessages;

        public int SortIndex => 0;

        public ConcurrentQueue<int> Processed { get; } = new();

        public TaskCompletionSource<bool> AllStarted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> AllProcessed { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Reset(int expectedMessages)
        {
            _release =
            [
                .. Enumerable.Range(0, degreeOfParallelism)
                    .Select(_ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously))
            ];
            _started = 0;
            _expectedMessages = expectedMessages;
            Processed.Clear();
            AllStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            AllProcessed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Release(int channel) => _release[channel].TrySetResult(true);

        public void ReleaseAll()
        {
            foreach (TaskCompletionSource<bool> release in _release)
            {
                release.TrySetResult(true);
            }
        }

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            using (context)
            {
                int number = context.Envelope.RawMessage!.ReadByte();

                if (number < degreeOfParallelism)
                {
                    if (Interlocked.Increment(ref _started) == degreeOfParallelism)
                        AllStarted.TrySetResult(true);

                    await _release[number].Task.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
                }

                await context.Consumer.CommitAsync(context.Envelope.BrokerMessageIdentifier);
                Processed.Enqueue(number);

                if (Processed.Count == _expectedMessages)
                    AllProcessed.TrySetResult(true);
            }
        }
    }
}
