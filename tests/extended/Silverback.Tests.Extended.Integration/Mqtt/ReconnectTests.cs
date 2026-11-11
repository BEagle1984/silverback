// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class ReconnectTests : BrokerHarnessTests<MqttTestHarness>
{
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public ReconnectTests(MqttFixture fixture, ITestOutputHelper output)
        : base(new MqttTestHarness("reconnect", output))
    {
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, false, false)]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, true, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, false, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true, false)]
    public async Task Reconnect_ShouldResumeConsumption_AndReplayOnlyPersistentDeliveries(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool persistentSession,
        bool blockWriter)
    {
        TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> blockedDelivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> blockedReplayProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> buffered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> disconnectingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> replayProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> freshProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<int> processed = new();
        ConcurrentQueue<(int Number, int Connection)> received = new();
        int connection = 1;
        bool sessionPresent = false;

        Harness.NativeClient.ConnectedAsync += args =>
        {
            sessionPresent = args.ConnectResult.IsSessionPresent;

            return Task.CompletedTask;
        };

        Harness.NativeClient.ApplicationMessageReceivedAsync += args =>
        {
            ReconnectMessage message = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!;
            received.Enqueue((message.Number, connection));

            if (message.Number == 3 && connection == 1)
                blockedDelivery.TrySetResult(true);

            return Task.CompletedTask;
        };

        Harness.AddClient(client =>
        {
            client.DisableParallelProcessing()
                .LimitBackpressure(1)
                .Consume<ReconnectMessage>(endpoint => endpoint
                    .ConsumeFrom(Harness.Prefix)
                    .WithQualityOfServiceLevel(qualityOfServiceLevel)
                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()));

            if (persistentSession)
            {
                if (blockWriter)
                    client.RequestPersistentSession(TimeSpan.FromMinutes(5));
                else
                    client.RequestPersistentSession();
            }
        });
        Harness.ConfigureSilverback(silverback => silverback
            .AddDelegateSubscriber<ReconnectMessage>(async ValueTask (message) =>
            {
                if (message.Number == 0)
                {
                    firstStarted.TrySetResult(true);
                    await releaseFirst.Task.WaitAsync(Harness.Timeout);
                }

                processed.Enqueue(message.Number);

                if (message.Number == 1)
                    replayProcessed.TrySetResult(true);
                else if (message.Number == 2)
                    freshProcessed.TrySetResult(true);
                else if (message.Number == 3)
                    blockedReplayProcessed.TrySetResult(true);
            }));
        Harness.OnDispose(() =>
        {
            releaseFirst.TrySetResult(true);
            Harness.Output.WriteLine($"QoS={qualityOfServiceLevel}; persistent={persistentSession}; sessionPresent={sessionPresent}");
            Harness.Output.WriteLine($"Received: {string.Join(", ", received)}; processed: {string.Join(", ", processed)}");
        });

        await Harness.InitializeAsync();
        Harness.Consumer.Client.Disconnecting.AddHandler(_ =>
        {
            disconnectingStarted.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        // Register after Silverback so this gate confirms its receive callback has buffered the delivery
        Harness.NativeClient.ApplicationMessageReceivedAsync += args =>
        {
            ReconnectMessage message = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!;

            if (message.Number == 1 && connection == 1)
                buffered.TrySetResult(true);

            return Task.CompletedTask;
        };

        await Harness.ConnectAsync();
        TestMqttClient producer = await Harness.CreateClientAsync("producer");

        await PublishAsync(0);
        await firstStarted.Task.WaitAsync(Harness.Timeout);
        await PublishAsync(1);
        await buffered.Task.WaitAsync(Harness.Timeout);

        if (blockWriter)
        {
            await PublishAsync(3);
            await blockedDelivery.Task.WaitAsync(Harness.Timeout);
        }

        Task disconnecting = Harness.Consumer.Client.DisconnectAsync().AsTask();

        await disconnectingStarted.Task.WaitAsync(Harness.Timeout);
        releaseFirst.TrySetResult(true);
        await disconnecting.WaitAsync(Harness.Timeout);

        processed.ShouldBe([0]);

        connection = 2;
        await Harness.ReconnectAsync();

        sessionPresent.ShouldBe(persistentSession);

        bool shouldReplay = persistentSession && qualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce;

        if (shouldReplay)
        {
            await replayProcessed.Task.WaitAsync(Harness.Timeout);
            received.ShouldContain((1, 2));

            if (blockWriter)
            {
                await blockedReplayProcessed.Task.WaitAsync(Harness.Timeout);
                received.ShouldContain((3, 2));
            }
        }

        await PublishAsync(2);
        await freshProcessed.Task.WaitAsync(Harness.Timeout);
        await Harness.Consumer.StopAsync().AsTask().WaitAsync(Harness.Timeout);

        int[] expected = [0, 2];

        if (shouldReplay)
            expected = blockWriter ? [0, 1, 3, 2] : [0, 1, 2];

        processed.ShouldBe(expected);
        received.ShouldContain((2, 2));

        Task PublishAsync(int number) =>
            producer.PublishJsonAsync(Harness.Prefix, new ReconnectMessage { Number = number }, qualityOfServiceLevel);
    }
}
