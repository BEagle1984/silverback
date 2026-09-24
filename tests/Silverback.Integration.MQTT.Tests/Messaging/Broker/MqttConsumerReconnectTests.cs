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

public class MqttConsumerReconnectTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
    public async Task ReconnectAsync_ShouldConsumeNewMessages_WhenOldConnectionHasBufferedDeliveries(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool receiveBeforeConnected)
    {
        await using ConsumerHarness harness = new(qualityOfServiceLevel);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0);
        await harness.Behavior.FirstStarted.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(1);

        Task disconnecting = harness.Client.DisconnectAsync().AsTask();

        await harness.Behavior.Stopping.Task.WaitAsync(Timeout);
        harness.Behavior.ReleaseFirst.TrySetResult(true);
        await disconnecting.WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0]);
        harness.Acknowledged.Select(acknowledgement => acknowledgement.Message).ShouldBe([0]);

        if (receiveBeforeConnected)
            harness.ReceiveBeforeConnected(2);

        await harness.ConnectAsync();

        if (!receiveBeforeConnected)
            await harness.DeliverAsync(2);

        await harness.Behavior.FreshMessageProcessed.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0, 2]);
        harness.Acknowledged.ShouldAllBe(acknowledgement => acknowledgement.ReceivedConnection == acknowledgement.AcknowledgedConnection);
        harness.Acknowledged.ShouldBe(
        [
            new Acknowledgement(0, 1, 1),
            new Acknowledgement(2, 2, 2)
        ]);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldReleaseBlockedDelivery_BeforeDisconnectingNativeClient()
    {
        await using ConsumerHarness harness = new(MqttQualityOfServiceLevel.AtLeastOnce);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0);
        await harness.Behavior.FirstStarted.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(1);

        Task delivering = harness.DeliverAsync(2);
        delivering.IsCompleted.ShouldBeFalse();
        harness.PendingReceive = delivering;

        Task disconnecting = harness.Client.DisconnectAsync().AsTask();

        await harness.Behavior.Stopping.Task.WaitAsync(Timeout);
        harness.Behavior.ReleaseFirst.TrySetResult(true);
        await disconnecting.WaitAsync(Timeout);
        await delivering;
        harness.Received.Last().ProcessingFailed.ShouldBeTrue();
        harness.Received.Last().AutoAcknowledge.ShouldBeFalse();

        harness.Behavior.Processed.ShouldBe([0]);
        harness.Acknowledged.Select(acknowledgement => acknowledgement.Message).ShouldBe([0]);

        await harness.ConnectAsync();
        await harness.DeliverAsync(2);
        await harness.Behavior.FreshMessageProcessed.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0, 2]);
        harness.Acknowledged.ShouldAllBe(acknowledgement => acknowledgement.ReceivedConnection == acknowledgement.AcknowledgedConnection);
        harness.Acknowledged.ShouldBe(
        [
            new Acknowledgement(0, 1, 1),
            new Acknowledgement(2, 2, 2)
        ]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Consume_ShouldUseEveryChannel(int degreeOfParallelism)
    {
        await using ConsumerHarness harness = new(MqttQualityOfServiceLevel.AtLeastOnce, degreeOfParallelism);
        TaskCompletionSource<bool> allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        harness.Behavior.ExpectedMessages = degreeOfParallelism;
        harness.Behavior.ReleaseFirst.TrySetResult(true);
        harness.Behavior.BeforeProcessing = async () =>
        {
            if (Interlocked.Increment(ref started) == degreeOfParallelism)
                allStarted.TrySetResult(true);

            await release.Task.WaitAsync(Timeout);
        };

        try
        {
            await harness.ConnectAsync();

            for (byte number = 0; number < degreeOfParallelism; number++)
            {
                await harness.DeliverAsync(number);
            }

            await allStarted.Task.WaitAsync(Timeout);
            harness.Behavior.Processed.ShouldBeEmpty();
            release.TrySetResult(true);
            await harness.Behavior.AllProcessed.Task.WaitAsync(Timeout);
            await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

            harness.Behavior.Processed.Order().ShouldBe(Enumerable.Range(0, degreeOfParallelism));
        }
        finally
        {
            release.TrySetResult(true);
        }
    }

    [Fact]
    public async Task StartAsync_ShouldPreserveBufferedDeliveries_WhenConnectionWasNotClosed()
    {
        await using ConsumerHarness harness = new(MqttQualityOfServiceLevel.AtLeastOnce);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0);
        await harness.Behavior.FirstStarted.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(1);

        Task stopping = harness.Consumer.StopAsync().AsTask();

        await harness.Behavior.Stopping.Task.WaitAsync(Timeout);
        harness.Behavior.ReleaseFirst.TrySetResult(true);
        await stopping.WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0]);

        await harness.Consumer.StartAsync();
        await harness.Behavior.BufferedMessageProcessed.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(2);
        await harness.Behavior.FreshMessageProcessed.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0, 1, 2]);
        harness.Acknowledged.ShouldBe(
        [
            new Acknowledgement(0, 1, 1),
            new Acknowledgement(1, 1, 1),
            new Acknowledgement(2, 1, 1)
        ]);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
    public async Task ConnectionLost_ShouldStopOldReaderAndDiscardBufferedDeliveries_BeforeReconnecting(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool blockWriter)
    {
        await using ConsumerHarness harness = new(qualityOfServiceLevel);
        await harness.ConnectAsync();
        await harness.DeliverAsync(0);
        await harness.Behavior.FirstStarted.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(1);

        Task delivering = blockWriter ? harness.DeliverAsync(2) : Task.CompletedTask;
        harness.PendingReceive = delivering;
        harness.LoseConnection();

        await Task.WhenAny(harness.Behavior.Stopping.Task, harness.Reconnecting.Task).WaitAsync(Timeout);
        harness.Behavior.Stopping.Task.IsCompleted.ShouldBeTrue();
        harness.Reconnecting.Task.IsCompleted.ShouldBeFalse();

        if (blockWriter)
        {
            await delivering.WaitAsync(Timeout);
            harness.Received.Last().ProcessingFailed.ShouldBeTrue();
        }

        harness.Behavior.ReleaseFirst.TrySetResult(true);
        await harness.Reconnected.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(2);
        await harness.Behavior.FreshMessageProcessed.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0, 2]);
        harness.Acknowledged.ShouldAllBe(acknowledgement => acknowledgement.ReceivedConnection == acknowledgement.AcknowledgedConnection);
        harness.Acknowledged.ShouldBe([new Acknowledgement(2, 2, 2)]);
        harness.Disconnections.ShouldBe(1);
    }

    [Fact]
    public async Task ConnectionLost_ShouldDiscardEarlyDelivery_WhenConnectionDropsBeforeConnectedNotification()
    {
        await using ConsumerHarness harness = new(MqttQualityOfServiceLevel.AtLeastOnce);
        harness.ReceiveBeforeConnected(1, true);

        await harness.ConnectAsync();
        await harness.Reconnected.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(2);
        await harness.Behavior.FreshMessageProcessed.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([2]);
        harness.Acknowledged.ShouldBe([new Acknowledgement(2, 2, 2)]);
        harness.Disconnections.ShouldBe(1);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task CommitAsync_ShouldAllowRecovery_WhenConnectionIsLostDuringAcknowledgement(MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        await using ConsumerHarness harness = new(qualityOfServiceLevel);
        harness.Behavior.ReleaseFirst.TrySetResult(true);
        harness.LoseConnectionDuringAcknowledgement();
        await harness.ConnectAsync();

        await harness.DeliverAsync(0);
        await harness.Reconnected.Task.WaitAsync(Timeout);
        await harness.DeliverAsync(2);
        await harness.Behavior.FreshMessageProcessed.Task.WaitAsync(Timeout);
        await harness.Consumer.StopAsync().AsTask().WaitAsync(Timeout);

        harness.Behavior.Processed.ShouldBe([0, 2]);
        harness.Acknowledged.ShouldBe([new Acknowledgement(2, 2, 2)]);
        harness.Disconnections.ShouldBe(1);
    }

    [Fact]
    public async Task CommitAsync_ShouldPreserveQoS2AcknowledgementFailure_WhenTransportIsStillConnected()
    {
        await using ConsumerHarness harness = new(MqttQualityOfServiceLevel.ExactlyOnce);
        harness.Behavior.ReleaseFirst.TrySetResult(true);
        InvalidOperationException failure = new("Acknowledgement failed while still connected.");
        harness.BeforeAcknowledge = () => throw failure;
        await harness.ConnectAsync();

        await harness.DeliverAsync(0);
        Exception actualFailure = await harness.Behavior.CommitFailure.Task.WaitAsync(Timeout);

        actualFailure.ShouldBeSameAs(failure);
        harness.Acknowledged.ShouldBeEmpty();
    }

    private sealed class ConsumerHarness : IAsyncDisposable
    {
        private readonly IMqttClient _nativeClient = Substitute.For<IMqttClient>();

        private readonly MqttQualityOfServiceLevel _qualityOfServiceLevel;

        private Func<MqttApplicationMessageReceivedEventArgs, Task> _messageReceived = null!;

        private TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _connection;

        private bool _isConnected;

        public ConsumerHarness(MqttQualityOfServiceLevel qualityOfServiceLevel, int degreeOfParallelism = 1)
        {
            _qualityOfServiceLevel = qualityOfServiceLevel;
            _nativeClient.IsConnected.Returns(_ => Volatile.Read(ref _isConnected));

            _nativeClient.When(client => client.ApplicationMessageReceivedAsync += Arg.Any<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
                .Do(call => _messageReceived = call.Arg<Func<MqttApplicationMessageReceivedEventArgs, Task>>());

            _nativeClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>()).Returns(async _ =>
            {
                if (Volatile.Read(ref _connection) > 0)
                    Reconnecting.TrySetResult(true);

                await PendingReceive;
                Interlocked.Increment(ref _connection);
                Volatile.Write(ref _isConnected, true);

                if (BeforeConnected != null)
                    await BeforeConnected();

                return new MqttClientConnectResult();
            });

            _nativeClient.DisconnectAsync(Arg.Any<MqttClientDisconnectOptions>(), Arg.Any<CancellationToken>()).Returns(async _ =>
            {
                Volatile.Write(ref _isConnected, false);
                await PendingReceive;
            });

            MqttClientConfiguration configuration = new MqttClientConfigurationBuilder(Substitute.For<IServiceProvider>())
                .ConnectViaTcp("reconnect-test-broker")
                .WithClientId("reconnect-test")
                .EnableParallelProcessing(degreeOfParallelism)
                .Consume(endpoint => endpoint.ConsumeFrom("topic"))
                .Build();
            Client = new MqttClientWrapper(
                "reconnect-test",
                _nativeClient,
                configuration,
                Substitute.For<IBrokerClientCallbacksInvoker>(),
                Substitute.For<ISilverbackLogger>());
            Client.Subscribed.AddHandler(_ =>
            {
                _subscribed.TrySetResult(true);

                if (Volatile.Read(ref _connection) > 1)
                    Reconnected.TrySetResult(true);

                return ValueTask.CompletedTask;
            });

            IBrokerBehaviorsProvider<IConsumerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
            behaviors.GetBehaviorsList().Returns([Behavior]);
            Client.Disconnected.AddHandler(_ =>
            {
                Disconnections++;

                return ValueTask.CompletedTask;
            });

            Consumer = new MqttConsumer(
                "reconnect-test",
                Client,
                configuration,
                behaviors,
                Substitute.For<IServiceProvider>(),
                Substitute.For<ISilverbackLogger<MqttConsumer>>());
        }

        public MqttClientWrapper Client { get; }

        public MqttConsumer Consumer { get; }

        public BlockingBehavior Behavior { get; } = new();

        public ConcurrentQueue<Acknowledgement> Acknowledged { get; } = new();

        public ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> Received { get; } = new();

        public Task PendingReceive { get; set; } = Task.CompletedTask;

        public Action? BeforeAcknowledge { get; set; }

        public TaskCompletionSource<bool> Reconnecting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Reconnected { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Disconnections { get; private set; }

        private Func<Task>? BeforeConnected { get; set; }

        public void LoseConnection() => Volatile.Write(ref _isConnected, false);

        public void LoseConnectionDuringAcknowledgement() => BeforeAcknowledge = () =>
        {
            BeforeAcknowledge = null;
            LoseConnection();
            throw new InvalidOperationException("Connection lost during acknowledgement.");
        };

        public void ReceiveBeforeConnected(byte number, bool loseConnection = false) => BeforeConnected = async () =>
        {
            BeforeConnected = null;
            await DeliverAsync(number);

            if (loseConnection)
                LoseConnection();
        };

        public async Task ConnectAsync()
        {
            _subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Client.ConnectAsync().AsTask().WaitAsync(Timeout);
            await _subscribed.Task.WaitAsync(Timeout);
        }

        public Task DeliverAsync(byte number)
        {
            int connection = Volatile.Read(ref _connection);
            MqttApplicationMessageReceivedEventArgs eventArgs = new(
                "reconnect-test",
                new MqttApplicationMessage
                {
                    Topic = "topic",
                    Payload = new ReadOnlySequence<byte>([number]),
                    QualityOfServiceLevel = _qualityOfServiceLevel
                },
                new MqttPublishPacket { QualityOfServiceLevel = _qualityOfServiceLevel },
                (_, _) =>
                {
                    if (!Volatile.Read(ref _isConnected))
                        throw new InvalidOperationException("The connection was lost before acknowledging the delivery.");

                    BeforeAcknowledge?.Invoke();
                    Acknowledged.Enqueue(new Acknowledgement(number, connection, Volatile.Read(ref _connection)));

                    return Task.CompletedTask;
                });

            Received.Enqueue(eventArgs);

            return _messageReceived(eventArgs).WaitAsync(Timeout);
        }

        public async ValueTask DisposeAsync()
        {
            Behavior.ReleaseFirst.TrySetResult(true);
            BeforeConnected = null;
            PendingReceive = Task.CompletedTask;
            await Client.DisconnectAsync().AsTask().WaitAsync(Timeout);
            Consumer.Dispose();
            await Client.DisposeAsync();
            _nativeClient.Dispose();
        }
    }

    private sealed class BlockingBehavior : IConsumerBehavior
    {
        public int SortIndex => 0;

        public Func<Task>? BeforeProcessing { get; set; }

        public int ExpectedMessages { get; set; } = 3;

        public TaskCompletionSource<Exception> CommitFailure { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> AllProcessed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Stopping { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> BufferedMessageProcessed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> FreshMessageProcessed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentQueue<int> Processed { get; } = new();

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            using (context)
            {
                int number = context.Envelope.RawMessage!.ReadByte();

                if (BeforeProcessing != null)
                    await BeforeProcessing();

                if (number == 0)
                {
                    using CancellationTokenRegistration registration = cancellationToken.Register(() => Stopping.TrySetResult(true));
                    FirstStarted.TrySetResult(true);
                    await ReleaseFirst.Task.WaitAsync(Timeout, CancellationToken.None);
                }

                try
                {
                    await context.Consumer.CommitAsync(context.Envelope.BrokerMessageIdentifier);
                }
                catch (Exception ex)
                {
                    CommitFailure.TrySetResult(ex);
                    throw;
                }

                Processed.Enqueue(number);

                if (Processed.Count == ExpectedMessages)
                    AllProcessed.TrySetResult(true);

                if (number == 1)
                    BufferedMessageProcessed.TrySetResult(true);
                else if (number == 2)
                    FreshMessageProcessed.TrySetResult(true);
            }
        }
    }

    private sealed record Acknowledgement(int Message, int ReceivedConnection, int AcknowledgedConnection);
}
