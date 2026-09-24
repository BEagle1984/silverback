// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Extended.Stress.TestHost.Mqtt;
using Xunit;

namespace Silverback.Tests.Extended.Stress.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Stress")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Mqtt")]
public class ShutdownTests(MqttFixture fixture)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, true)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, false)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
    public async Task StopAsync_ShouldAllowSubscriberToPublishThroughSharedClient(
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool fillInputBuffer)
    {
        _ = fixture;

        string prefix = "stress-shutdown-" + Guid.NewGuid().ToString("N");
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> published = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> buffered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> blockedDelivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> keepAliveReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ShutdownReply> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int processed = 0;
        int pingResponses = 0;
        int disconnects = 0;

        using NativeClientFactory clientFactory = new();
        clientFactory.Client.InspectPacketAsync += args =>
        {
            // Observe actual PINGRESP packets while shutdown holds the subscriber open
            if (stopping.Task.IsCompleted && args.Direction == MqttPacketFlowDirection.Inbound &&
                args.Buffer is [0xd0, 0x00] && Interlocked.Increment(ref pingResponses) == 2)
            {
                keepAliveReceived.TrySetResult(true);
            }

            return Task.CompletedTask;
        };

        clientFactory.Client.DisconnectedAsync += _ =>
        {
            Interlocked.Increment(ref disconnects);

            return Task.CompletedTask;
        };

        clientFactory.Client.ApplicationMessageReceivedAsync += args =>
        {
            if (JsonSerializer.Deserialize<ShutdownRequest>(args.ApplicationMessage.Payload.ToArray())!.Number == 2)
                blockedDelivery.TrySetResult(true);

            return Task.CompletedTask;
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp(MqttFixture.BrokerHost)
                .AddClient(client => client
                    .WithClientId(prefix)
                    .SendKeepAlive(TimeSpan.FromSeconds(4))
                    .DisableParallelProcessing()
                    .Consume<ShutdownRequest>(endpoint => endpoint
                        .ConsumeFrom(prefix + "-input")
                        .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))
                    .Produce<ShutdownReply>(endpoint => endpoint
                        .ProduceTo(prefix + "-output")
                        .WithQualityOfServiceLevel(qualityOfServiceLevel))))
            .AddDelegateSubscriber<ShutdownRequest, IPublisher, CancellationToken>(
                async Task (message, publisher, cancellationToken) =>
                {
                    Interlocked.Increment(ref processed);
                    using CancellationTokenRegistration registration = cancellationToken.Register(() => stopping.TrySetResult(true));
                    started.TrySetResult(true);
                    await release.Task.WaitAsync(Timeout, CancellationToken.None);

                    await publisher.PublishAsync(new ShutdownReply { Number = message.Number }, cancellationToken: CancellationToken.None);
                    published.TrySetResult(true);
                });
        builder.Services.AddSingleton<IMqttNetClientFactory>(clientFactory);

        using IHost host = builder.Build();
        await host.Services.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        MqttConsumer consumer = host.Services.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        consumer.Client.Subscribed.AddHandler(_ =>
        {
            subscribed.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        clientFactory.Client.ApplicationMessageReceivedAsync += args =>
        {
            if (JsonSerializer.Deserialize<ShutdownRequest>(args.ApplicationMessage.Payload.ToArray())!.Number == 1)
                buffered.TrySetResult(true);

            return Task.CompletedTask;
        };

        using IMqttClient observer = new MqttClientFactory().CreateMqttClient();
        observer.ApplicationMessageReceivedAsync += args =>
        {
            received.TrySetResult(JsonSerializer.Deserialize<ShutdownReply>(args.ApplicationMessage.Payload.ToArray())!);

            return Task.CompletedTask;
        };
        Task? shutdown = null;

        try
        {
            await observer.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(prefix + "-observer")
                .Build());
            await observer.SubscribeAsync(prefix + "-output", qualityOfServiceLevel);
            await host.StartAsync();
            await subscribed.Task.WaitAsync(Timeout);

            await PublishInputAsync(0);
            await started.Task.WaitAsync(Timeout);

            if (fillInputBuffer)
            {
                await PublishInputAsync(1);
                await buffered.Task.WaitAsync(Timeout);
                await PublishInputAsync(2);
                await blockedDelivery.Task.WaitAsync(Timeout);
            }

            shutdown = host.StopAsync();
            await stopping.Task.WaitAsync(Timeout);
            await keepAliveReceived.Task.WaitAsync(Timeout);

            Volatile.Read(ref disconnects).ShouldBe(0);
            Volatile.Read(ref pingResponses).ShouldBeGreaterThanOrEqualTo(2);
            shutdown.IsCompleted.ShouldBeFalse();
            clientFactory.Client.IsConnected.ShouldBeTrue();

            release.TrySetResult(true);
            await published.Task.WaitAsync(Timeout);
            (await received.Task.WaitAsync(Timeout)).Number.ShouldBe(0);
            await shutdown.WaitAsync(Timeout);

            processed.ShouldBe(1);
            consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
            clientFactory.Client.IsConnected.ShouldBeFalse();
        }
        finally
        {
            release.TrySetResult(true);
            await (shutdown ?? host.StopAsync()).WaitAsync(Timeout);

            if (observer.IsConnected)
                await observer.DisconnectAsync();
        }

        Task PublishInputAsync(int number) => observer.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(prefix + "-input")
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new ShutdownRequest { Number = number }))
            .Build()).WaitAsync(Timeout);
    }

    private sealed class NativeClientFactory : IMqttNetClientFactory, IDisposable
    {
        public IMqttClient Client { get; } = new MqttClientFactory().CreateMqttClient();

        public IMqttClient CreateClient() => Client;

        public void Dispose() => Client.Dispose();
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
