// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Stress.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Stress.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Stress")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Mqtt")]
public class ReconnectTests(MqttFixture fixture, ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

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
        _ = fixture;

        string prefix = "stress-reconnect-" + Guid.NewGuid().ToString("N");
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

        using NativeClientFactory clientFactory = new();
        clientFactory.Client.ConnectedAsync += args =>
        {
            sessionPresent = args.ConnectResult.IsSessionPresent;

            return Task.CompletedTask;
        };

        clientFactory.Client.ApplicationMessageReceivedAsync += args =>
        {
            ReconnectMessage message = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!;
            received.Enqueue((message.Number, connection));

            if (message.Number == 3 && connection == 1)
                blockedDelivery.TrySetResult(true);

            return Task.CompletedTask;
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        IServiceCollection services = builder.Services;
        services.AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp(MqttFixture.BrokerHost)
                .AddClient(client =>
                {
                    client.WithClientId(prefix)
                        .DisableParallelProcessing()
                        .LimitBackpressure(1)
                        .Consume<ReconnectMessage>(endpoint => endpoint
                            .ConsumeFrom(prefix)
                            .WithQualityOfServiceLevel(qualityOfServiceLevel)
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()));

                    if (persistentSession)
                    {
                        if (blockWriter)
                            client.RequestPersistentSession(TimeSpan.FromMinutes(5));
                        else
                            client.RequestPersistentSession();
                    }
                }))
            .AddDelegateSubscriber<ReconnectMessage>(async ValueTask (message) =>
            {
                if (message.Number == 0)
                {
                    firstStarted.TrySetResult(true);
                    await releaseFirst.Task.WaitAsync(Timeout);
                }

                processed.Enqueue(message.Number);

                if (message.Number == 1)
                    replayProcessed.TrySetResult(true);
                else if (message.Number == 2)
                    freshProcessed.TrySetResult(true);
                else if (message.Number == 3)
                    blockedReplayProcessed.TrySetResult(true);
            });
        services.AddSingleton<IMqttNetClientFactory>(clientFactory);

        using IHost host = builder.Build();
        IServiceProvider serviceProvider = host.Services;
        IBrokerClientsConnector connector = serviceProvider.GetRequiredService<IBrokerClientsConnector>();
        await connector.InitializeAsync();
        MqttConsumer consumer = serviceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        TaskCompletionSource<bool> subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.Client.Subscribed.AddHandler(_ =>
        {
            subscribed.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        consumer.Client.Disconnecting.AddHandler(_ =>
        {
            disconnectingStarted.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        // Register after Silverback so this gate confirms its receive callback has buffered the delivery
        clientFactory.Client.ApplicationMessageReceivedAsync += args =>
        {
            ReconnectMessage message = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!;

            if (message.Number == 1 && connection == 1)
                buffered.TrySetResult(true);

            return Task.CompletedTask;
        };

        using IMqttClient producer = new MqttClientFactory().CreateMqttClient();

        try
        {
            await connector.ConnectAsync().AsTask().WaitAsync(Timeout);
            await subscribed.Task.WaitAsync(Timeout);
            await producer.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(prefix + "-producer")
                .Build());

            await PublishAsync(0);
            await firstStarted.Task.WaitAsync(Timeout);
            await PublishAsync(1);
            await buffered.Task.WaitAsync(Timeout);

            if (blockWriter)
            {
                await PublishAsync(3);
                await blockedDelivery.Task.WaitAsync(Timeout);
            }

            Task disconnecting = consumer.Client.DisconnectAsync().AsTask();

            await disconnectingStarted.Task.WaitAsync(Timeout);
            releaseFirst.TrySetResult(true);
            await disconnecting.WaitAsync(Timeout);

            processed.ShouldBe([0]);

            connection = 2;
            subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await consumer.Client.ConnectAsync().AsTask().WaitAsync(Timeout);
            await subscribed.Task.WaitAsync(Timeout);

            sessionPresent.ShouldBe(persistentSession);

            bool shouldReplay = persistentSession && qualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce;

            if (shouldReplay)
            {
                await replayProcessed.Task.WaitAsync(Timeout);
                received.ShouldContain((1, 2));

                if (blockWriter)
                {
                    await blockedReplayProcessed.Task.WaitAsync(Timeout);
                    received.ShouldContain((3, 2));
                }
            }

            await PublishAsync(2);
            await freshProcessed.Task.WaitAsync(Timeout);
            await consumer.StopAsync().AsTask().WaitAsync(Timeout);

            int[] expected = [0, 2];

            if (shouldReplay)
                expected = blockWriter ? [0, 1, 3, 2] : [0, 1, 2];

            processed.ShouldBe(expected);
            received.ShouldContain((2, 2));
        }
        finally
        {
            releaseFirst.TrySetResult(true);
            output.WriteLine($"QoS={qualityOfServiceLevel}; persistent={persistentSession}; sessionPresent={sessionPresent}");
            output.WriteLine($"Received: {string.Join(", ", received)}; processed: {string.Join(", ", processed)}");
            await connector.DisconnectAsync().AsTask().WaitAsync(Timeout);

            if (producer.IsConnected)
                await producer.DisconnectAsync();

            if (persistentSession)
            {
                // Delete the test session, including those now configured without an expiry
                using IMqttClient cleanupClient = new MqttClientFactory().CreateMqttClient();
                await cleanupClient.ConnectAsync(new MqttClientOptionsBuilder()
                    .WithTcpServer(MqttFixture.BrokerHost)
                    .WithClientId(prefix)
                    .WithCleanSession()
                    .WithSessionExpiryInterval(0)
                    .Build()).WaitAsync(Timeout);
                await cleanupClient.DisconnectAsync().WaitAsync(Timeout);
            }
        }

        Task PublishAsync(int number) => producer.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(prefix)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new ReconnectMessage { Number = number }))
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .Build()).WaitAsync(Timeout);
    }

    private sealed class NativeClientFactory : IMqttNetClientFactory, IDisposable
    {
        public IMqttClient Client { get; } = new MqttClientFactory().CreateMqttClient();

        public IMqttClient CreateClient() => Client;

        public void Dispose() => Client.Dispose();
    }
}
