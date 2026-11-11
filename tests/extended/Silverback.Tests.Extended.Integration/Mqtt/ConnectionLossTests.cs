// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class ConnectionLossTests : BrokerHarnessTests<MqttTestHarness>
{
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public ConnectionLossTests(MqttFixture fixture, ITestOutputHelper output)
        : base(new MqttTestHarness("loss", output))
    {
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, false, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true, true)]
    public async Task ConnectionLost_ShouldCleanUpOldDeliveryAndResumeAfterSocketIsCut(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool persistentSession,
        bool publishFromSubscriber)
    {
        string outputTopic = Harness.Prefix + "-output";
        TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> buffered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> freshProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> replyReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<(int Message, int Connection)> processed = new();
        int connection = 0;
        int disconnectNotifications = 0;
        bool sessionPresent = false;
        int failedPublishes = 0;

        DisconnectableTcpProxy proxy = Harness.UseDisconnectableProxy();

        Harness.NativeClient.ConnectedAsync += args =>
        {
            Interlocked.Increment(ref connection);
            sessionPresent = args.ConnectResult.IsSessionPresent;

            return Task.CompletedTask;
        };

        Harness.NativeClient.ApplicationMessageReceivedAsync += args =>
        {
            int number = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!.Number;

            if (number == 2 && connection == 1)
                blocked.TrySetResult(true);

            return Task.CompletedTask;
        };

        Harness.AddClient(client =>
        {
            client.DisableParallelProcessing()
                .LimitBackpressure(1)
                .Consume<ReconnectMessage>(endpoint => endpoint
                    .ConsumeFrom(Harness.Prefix)
                    .WithQualityOfServiceLevel(qualityOfServiceLevel)
                    .OnError(policy => policy.Retry(3))
                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))
                .Produce<ConnectionLossReply>(endpoint => endpoint
                    .ProduceTo(outputTopic)
                    .WithQualityOfServiceLevel(qualityOfServiceLevel));

            if (persistentSession)
                client.RequestPersistentSession(TimeSpan.FromMinutes(5));
        });
        Harness.ConfigureSilverback(silverback => silverback
            .AddDelegateSubscriber<ReconnectMessage, IPublisher, CancellationToken>(async Task (message, publisher, cancellationToken) =>
            {
                int receivedConnection = connection;

                if (message.Number == 0 && receivedConnection == 1)
                {
                    using CancellationTokenRegistration registration = cancellationToken.Register(() => stopping.TrySetResult(true));
                    firstStarted.TrySetResult(true);
                    await releaseFirst.Task.WaitAsync(Harness.Timeout, CancellationToken.None);
                }

                processed.Enqueue((message.Number, receivedConnection));

                if (publishFromSubscriber && message.Number == 0)
                {
                    try
                    {
                        await publisher.PublishAsync(new ConnectionLossReply { Number = 0 }, cancellationToken: CancellationToken.None);
                    }
                    catch (ProduceException)
                    {
                        Interlocked.Increment(ref failedPublishes);
                        throw;
                    }
                }

                if (message.Number == 3)
                    freshProcessed.TrySetResult(true);
            }));
        Harness.OnDispose(() =>
        {
            releaseFirst.TrySetResult(true);
            Harness.Output.WriteLine($"Connection={connection}; disconnected notifications={disconnectNotifications}; processed={string.Join(", ", processed)}");
        });

        await Harness.InitializeAsync();
        Harness.Consumer.Client.Disconnected.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnectNotifications);

            return ValueTask.CompletedTask;
        });

        // Register after Silverback so this gate confirms its receive callback has buffered the delivery
        Harness.NativeClient.ApplicationMessageReceivedAsync += args =>
        {
            int number = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!.Number;

            if (number == 1 && connection == 1)
                buffered.TrySetResult(true);

            return Task.CompletedTask;
        };

        TestMqttClient producer = await Harness.CreateClientAsync("producer");
        producer.Native.ApplicationMessageReceivedAsync += args =>
        {
            JsonSerializer.Deserialize<ConnectionLossReply>(args.ApplicationMessage.Payload.ToArray())!.Number.ShouldBe(0);
            replyReceived.TrySetResult(true);

            return Task.CompletedTask;
        };
        await producer.SubscribeAsync(outputTopic, qualityOfServiceLevel);
        await Harness.ConnectAsync();

        await PublishAsync(0);
        await firstStarted.Task.WaitAsync(Harness.Timeout);
        await PublishAsync(1);
        await buffered.Task.WaitAsync(Harness.Timeout);
        await PublishAsync(2);
        await blocked.Task.WaitAsync(Harness.Timeout);

        proxy.DropConnections();
        await stopping.Task.WaitAsync(Harness.Timeout);

        connection.ShouldBe(1);
        Harness.SubscriptionsCount.ShouldBe(1);

        releaseFirst.TrySetResult(true);
        await Harness.WaitUntilSubscribedAsync(2);
        await PublishAsync(3);
        await freshProcessed.Task.WaitAsync(Harness.Timeout);
        await Harness.Consumer.StopAsync().AsTask().WaitAsync(Harness.Timeout);

        if (publishFromSubscriber)
        {
            await replyReceived.Task.WaitAsync(Harness.Timeout);
            failedPublishes.ShouldBe(1);
        }

        disconnectNotifications.ShouldBe(1);
        sessionPresent.ShouldBe(persistentSession);
        (int, int)[] expected = persistentSession ? [(0, 1), (0, 2), (1, 2), (2, 2), (3, 2)] : [(0, 1), (3, 2)];
        processed.ToArray().ShouldBe(expected);

        Task PublishAsync(int number) =>
            producer.PublishJsonAsync(Harness.Prefix, new ReconnectMessage { Number = number }, qualityOfServiceLevel);
    }

    private sealed class ConnectionLossReply
    {
        public int Number { get; set; }
    }
}
