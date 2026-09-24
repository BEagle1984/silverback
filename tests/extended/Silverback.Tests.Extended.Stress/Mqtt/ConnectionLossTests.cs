// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
using Silverback.Messaging.Publishing;
using Silverback.Tests.Extended.Stress.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Stress.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Stress")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Mqtt")]
public class ConnectionLossTests(MqttFixture fixture, ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

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
        _ = fixture;

        string prefix = "stress-loss-" + Guid.NewGuid().ToString("N");
        TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> buffered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> reconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> freshProcessed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<(int Message, int Connection)> processed = new();
        int connection = 0;
        int disconnectNotifications = 0;
        bool sessionPresent = false;
        int failedPublishes = 0;
        TaskCompletionSource<bool> replyReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using DisconnectableTcpProxy proxy = new();
        using NativeClientFactory clientFactory = new();
        clientFactory.Client.ConnectedAsync += args =>
        {
            Interlocked.Increment(ref connection);
            sessionPresent = args.ConnectResult.IsSessionPresent;

            return Task.CompletedTask;
        };

        clientFactory.Client.ApplicationMessageReceivedAsync += args =>
        {
            int number = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!.Number;

            if (number == 2 && Volatile.Read(ref connection) == 1)
                blocked.TrySetResult(true);

            return Task.CompletedTask;
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole();
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("127.0.0.1", proxy.Port)
                .AddClient(client =>
                {
                    client.WithClientId(prefix)
                        .DisableParallelProcessing()
                        .LimitBackpressure(1)
                        .Consume<ReconnectMessage>(endpoint => endpoint
                            .ConsumeFrom(prefix)
                            .WithQualityOfServiceLevel(qualityOfServiceLevel)
                            .OnError(policy => policy.Retry(3))
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))
                        .Produce<ConnectionLossReply>(endpoint => endpoint
                            .ProduceTo(prefix + "-output")
                            .WithQualityOfServiceLevel(qualityOfServiceLevel));

                    if (persistentSession)
                        client.RequestPersistentSession(TimeSpan.FromMinutes(5));
                }))
            .AddDelegateSubscriber<ReconnectMessage, IPublisher, CancellationToken>(async Task (message, publisher, cancellationToken) =>
            {
                int receivedConnection = Volatile.Read(ref connection);

                if (message.Number == 0 && receivedConnection == 1)
                {
                    using CancellationTokenRegistration registration = cancellationToken.Register(() => stopping.TrySetResult(true));
                    firstStarted.TrySetResult(true);
                    await releaseFirst.Task.WaitAsync(Timeout, CancellationToken.None);
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
            });
        builder.Services.AddSingleton<IMqttNetClientFactory>(clientFactory);

        using IHost host = builder.Build();
        IBrokerClientsConnector connector = host.Services.GetRequiredService<IBrokerClientsConnector>();
        await connector.InitializeAsync();
        MqttConsumer consumer = host.Services.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        consumer.Client.Disconnected.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnectNotifications);

            return ValueTask.CompletedTask;
        });
        consumer.Client.Subscribed.AddHandler(_ =>
        {
            subscribed.TrySetResult(true);

            if (Volatile.Read(ref connection) > 1)
                reconnected.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        clientFactory.Client.ApplicationMessageReceivedAsync += args =>
        {
            int number = JsonSerializer.Deserialize<ReconnectMessage>(args.ApplicationMessage.Payload.ToArray())!.Number;

            if (number == 1 && Volatile.Read(ref connection) == 1)
                buffered.TrySetResult(true);

            return Task.CompletedTask;
        };

        using IMqttClient producer = new MqttClientFactory().CreateMqttClient();
        producer.ApplicationMessageReceivedAsync += args =>
        {
            JsonSerializer.Deserialize<ConnectionLossReply>(args.ApplicationMessage.Payload.ToArray())!.Number.ShouldBe(0);
            replyReceived.TrySetResult(true);

            return Task.CompletedTask;
        };

        try
        {
            await producer.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(prefix + "-producer")
                .Build());
            await producer.SubscribeAsync(prefix + "-output", qualityOfServiceLevel);
            await connector.ConnectAsync().AsTask().WaitAsync(Timeout);
            await subscribed.Task.WaitAsync(Timeout);

            await PublishAsync(0);
            await firstStarted.Task.WaitAsync(Timeout);
            await PublishAsync(1);
            await buffered.Task.WaitAsync(Timeout);
            await PublishAsync(2);
            await blocked.Task.WaitAsync(Timeout);

            proxy.DropConnections();
            await stopping.Task.WaitAsync(Timeout);

            connection.ShouldBe(1);
            reconnected.Task.IsCompleted.ShouldBeFalse();

            releaseFirst.TrySetResult(true);
            await reconnected.Task.WaitAsync(Timeout);
            await PublishAsync(3);
            await freshProcessed.Task.WaitAsync(Timeout);
            await consumer.StopAsync().AsTask().WaitAsync(Timeout);

            if (publishFromSubscriber)
            {
                await replyReceived.Task.WaitAsync(Timeout);
                failedPublishes.ShouldBe(1);
            }

            disconnectNotifications.ShouldBe(1);
            sessionPresent.ShouldBe(persistentSession);
            (int, int)[] expected = persistentSession ? [(0, 1), (0, 2), (1, 2), (2, 2), (3, 2)] : [(0, 1), (3, 2)];
            processed.ToArray().ShouldBe(expected);
        }
        finally
        {
            releaseFirst.TrySetResult(true);
            output.WriteLine($"Connection={connection}; disconnected notifications={disconnectNotifications}; processed={string.Join(", ", processed)}");
            await connector.DisconnectAsync().AsTask().WaitAsync(Timeout);

            if (producer.IsConnected)
                await producer.DisconnectAsync();
        }

        Task PublishAsync(int number) => producer.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(prefix)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(new ReconnectMessage { Number = number }))
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .Build()).WaitAsync(Timeout);
    }

    private sealed class ConnectionLossReply
    {
        public int Number { get; set; }
    }

    private sealed class NativeClientFactory : IMqttNetClientFactory, IDisposable
    {
        public IMqttClient Client { get; } = new MqttClientFactory().CreateMqttClient();

        public IMqttClient CreateClient() => Client;

        public void Dispose() => Client.Dispose();
    }
}
