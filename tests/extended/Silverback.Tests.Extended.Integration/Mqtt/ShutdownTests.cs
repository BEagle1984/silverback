// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.PacketInspection;
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
public class ShutdownTests : BrokerHarnessTests<MqttTestHarness>
{
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public ShutdownTests(MqttFixture fixture, ITestOutputHelper output)
        : base(new MqttTestHarness("shutdown", output))
    {
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
    public async Task StopAsync_ShouldAllowSubscriberToPublishThroughSharedClient(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool fillInputBuffer)
    {
        string inputTopic = Harness.Prefix + "-input";
        string outputTopic = Harness.Prefix + "-output";
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> published = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> buffered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> blockedDelivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> keepAliveReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ShutdownReply> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int processed = 0;
        int pingResponses = 0;
        int disconnects = 0;

        Harness.NativeClient.InspectPacketAsync += args =>
        {
            // Observe actual PINGRESP packets while shutdown holds the subscriber open
            if (stopping.Task.IsCompleted && args.Direction == MqttPacketFlowDirection.Inbound &&
                args.Buffer is [0xd0, 0x00] && Interlocked.Increment(ref pingResponses) == 2)
            {
                keepAliveReceived.TrySetResult(true);
            }

            return Task.CompletedTask;
        };

        Harness.NativeClient.DisconnectedAsync += _ =>
        {
            Interlocked.Increment(ref disconnects);

            return Task.CompletedTask;
        };

        Harness.NativeClient.ApplicationMessageReceivedAsync += args =>
        {
            if (JsonSerializer.Deserialize<ShutdownRequest>(args.ApplicationMessage.Payload.ToArray())!.Number == 2)
                blockedDelivery.TrySetResult(true);

            return Task.CompletedTask;
        };

        Harness.AddClient(client => client
            .SendKeepAlive(TimeSpan.FromSeconds(4))
            .DisableParallelProcessing()
            .LimitBackpressure(1)
            .Consume<ShutdownRequest>(endpoint => endpoint
                .ConsumeFrom(inputTopic)
                .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))
            .Produce<ShutdownReply>(endpoint => endpoint
                .ProduceTo(outputTopic)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)));
        Harness.ConfigureSilverback(silverback => silverback
            .AddDelegateSubscriber<ShutdownRequest, IPublisher, CancellationToken>(
                async Task (message, publisher, cancellationToken) =>
                {
                    Interlocked.Increment(ref processed);
                    using CancellationTokenRegistration registration = cancellationToken.Register(() => stopping.TrySetResult(true));
                    started.TrySetResult(true);
                    await release.Task.WaitAsync(Harness.Timeout, CancellationToken.None);

                    await publisher.PublishAsync(new ShutdownReply { Number = message.Number }, cancellationToken: CancellationToken.None);
                    published.TrySetResult(true);
                }));
        Harness.OnDispose(() => release.TrySetResult(true));

        await Harness.InitializeAsync();

        // Register after Silverback so this gate confirms its receive callback has buffered the delivery
        Harness.NativeClient.ApplicationMessageReceivedAsync += args =>
        {
            if (JsonSerializer.Deserialize<ShutdownRequest>(args.ApplicationMessage.Payload.ToArray())!.Number == 1)
                buffered.TrySetResult(true);

            return Task.CompletedTask;
        };

        TestMqttClient observer = await Harness.CreateClientAsync("observer");
        observer.Native.ApplicationMessageReceivedAsync += args =>
        {
            received.TrySetResult(JsonSerializer.Deserialize<ShutdownReply>(args.ApplicationMessage.Payload.ToArray())!);

            return Task.CompletedTask;
        };
        await observer.SubscribeAsync(outputTopic, qualityOfServiceLevel);
        await Harness.StartHostAsync();

        await observer.PublishJsonAsync(inputTopic, new ShutdownRequest { Number = 0 });
        await started.Task.WaitAsync(Harness.Timeout);

        if (fillInputBuffer)
        {
            await observer.PublishJsonAsync(inputTopic, new ShutdownRequest { Number = 1 });
            await buffered.Task.WaitAsync(Harness.Timeout);
            await observer.PublishJsonAsync(inputTopic, new ShutdownRequest { Number = 2 });
            await blockedDelivery.Task.WaitAsync(Harness.Timeout);
        }

        Task shutdown = Harness.StopHostAsync();
        await stopping.Task.WaitAsync(Harness.Timeout);
        await keepAliveReceived.Task.WaitAsync(Harness.Timeout);

        disconnects.ShouldBe(0);
        pingResponses.ShouldBeGreaterThanOrEqualTo(2);
        shutdown.IsCompleted.ShouldBeFalse();
        Harness.NativeClient.IsConnected.ShouldBeTrue();

        release.TrySetResult(true);
        await published.Task.WaitAsync(Harness.Timeout);
        (await received.Task.WaitAsync(Harness.Timeout)).Number.ShouldBe(0);
        await shutdown.WaitAsync(Harness.Timeout);

        processed.ShouldBe(1);
        Harness.Consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        Harness.NativeClient.IsConnected.ShouldBeFalse();
    }

    private sealed class ShutdownRequest
    {
        public int Number { get; set; }
    }

    private sealed class ShutdownReply
    {
        public int Number { get; set; }
    }
}
