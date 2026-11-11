// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class ChunkingTests
{
    private const int ChunkSize = 50;

    private const int MessagesCount = 12;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ITestOutputHelper _output;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public ChunkingTests(MqttFixture fixture, ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Chunking_ShouldReassembleAndAcknowledgeChunkedMessages(int maxDegreeOfParallelism)
    {
        string consumerClientId = "stress-chunk-" + Guid.NewGuid().ToString("N");
        string topic = consumerClientId;
        string observerClientId = consumerClientId + "-observer";
        ChunkMessage[] messages =
        [
            .. Enumerable.Range(1, MessagesCount).Select(number => new ChunkMessage
            {
                Number = number,
                Content = $"Message {number}: " + new string((char)('a' + number), 100)
            })
        ];
        ConcurrentQueue<ChunkMessage> consumed = new();
        ConcurrentQueue<ObservedChunk> chunks = new();

        using RecordingMqttClientFactory clientFactory = new();
        using RecordingLoggerProvider logs = new();
        using IHost host = CreateHost(topic, consumerClientId, maxDegreeOfParallelism, consumed, clientFactory, logs);
        IBrokerClientsConnector connector = host.Services.GetRequiredService<IBrokerClientsConnector>();

        // An independent client observes the chunks produced to the broker
        using IMqttClient observer = new MqttClientFactory().CreateMqttClient();
        observer.ApplicationMessageReceivedAsync += args =>
        {
            chunks.Enqueue(ObservedChunk.Create(args.ApplicationMessage));

            return Task.CompletedTask;
        };

        try
        {
            await connector.InitializeAsync();
            MqttConsumer consumer = host.Services.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
            Task subscribed = WaitUntilSubscribedAsync(consumer);

            await connector.ConnectAsync().AsTask().WaitAsync(Timeout);
            await subscribed.WaitAsync(Timeout);
            await observer.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(observerClientId)
                .Build()).WaitAsync(Timeout);
            await observer.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce).WaitAsync(Timeout);
            await EmqxCluster.WaitUntilRoutedAsync(topic, Timeout, consumerClientId, observerClientId);

            // Produced while the consumer is disconnected, so that the broker delivers the chunks of several messages at once (up to its
            // in-flight limit) when the session is resumed
            await consumer.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);
            await PublishAsync(host, messages);
            await WaitUntilAsync(() => chunks.Count(chunk => chunk.IsLastChunk) == MessagesCount, "the chunks to be produced");

            subscribed = WaitUntilSubscribedAsync(consumer);
            await consumer.Client.ConnectAsync().AsTask().WaitAsync(Timeout);
            await subscribed.WaitAsync(Timeout);
            await EmqxCluster.WaitUntilRoutedAsync(topic, Timeout, consumerClientId, observerClientId);

            await WaitUntilAsync(() => consumed.Count >= MessagesCount, "the chunked messages to be consumed");

            // Each chunk is acknowledged once its message is processed
            MqttClientRecorder recorder = clientFactory.GetRecorder(consumerClientId);
            await WaitUntilAsync(() => AreAllChunksAcknowledged(recorder, chunks.Count), "the chunks to be acknowledged");

            if (maxDegreeOfParallelism == 1)
                consumed.ShouldBe(messages);
            else
                consumed.ShouldBe(messages, ignoreOrder: true);

            // Each message was split into contiguous chunks, correlated by the chunked message id
            ObservedChunk[] observedChunks = [.. chunks];
            observedChunks.ShouldAllBe(chunk => chunk.PayloadLength <= ChunkSize && chunk.ChunkMessageId != null);
            observedChunks.Select(chunk => chunk.ChunkMessageId).Distinct().Count().ShouldBe(MessagesCount);
            int maxChunksPerMessage = VerifyContiguousChunks(observedChunks);

            recorder.IsSessionPresent.ShouldBeTrue();
            recorder.GetDeliveries().Count.ShouldBe(observedChunks.Length);

            // The chunks of several messages were waiting for acknowledgment at the same time
            recorder.MaxInFlight.ShouldBeGreaterThan(maxChunksPerMessage);
            logs.Errors.ShouldBeEmpty();

            // The broker doesn't replay any chunk when the session is resumed
            await consumer.Client.DisconnectAsync().AsTask().WaitAsync(Timeout);
            EmqxCluster.SessionReplay replay = await EmqxCluster.ResumeAndEndSessionAsync(consumerClientId, topic, Timeout);

            replay.IsSessionPresent.ShouldBeTrue();
            replay.Payloads.ShouldBeEmpty();
        }
        finally
        {
            _output.WriteLine($"Client={consumerClientId}; consumed: {string.Join(", ", consumed.Select(message => message.Number))}");
            _output.WriteLine($"Chunks: {string.Join(", ", chunks)}");
            logs.WriteTo(_output);

            try
            {
                await connector.DisconnectAsync().AsTask().WaitAsync(Timeout);

                if (observer.IsConnected)
                    await observer.DisconnectAsync().WaitAsync(Timeout);
            }
            finally
            {
                // Delete the test session, unless the verification ended it already
                await EmqxCluster.DeleteSessionAsync(consumerClientId, Timeout);
            }
        }
    }

    private static IHost CreateHost(
        string topic,
        string consumerClientId,
        int maxDegreeOfParallelism,
        ConcurrentQueue<ChunkMessage> consumed,
        RecordingMqttClientFactory clientFactory,
        ILoggerProvider logs)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders().AddProvider(logs);

        // A dedicated client consumes the chunked messages, so that the chunks of each message are contiguous
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp(MqttFixture.BrokerHost)
                .AddClient(client => client
                    .WithClientId(consumerClientId + "-producer")
                    .Produce<ChunkMessage>(endpoint => endpoint
                        .ProduceTo(topic)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize)))
                .AddClient(client => client
                    .WithClientId(consumerClientId)
                    .RequestPersistentSession(TimeSpan.FromMinutes(5))
                    .EnableParallelProcessing(maxDegreeOfParallelism)
                    .Consume<ChunkMessage>(endpoint => endpoint
                        .ConsumeFrom(topic)
                        .WithAtLeastOnceQoS()
                        .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader()))))
            .AddDelegateSubscriber<ChunkMessage>(consumed.Enqueue);

        builder.Services.AddSingleton<IMqttNetClientFactory>(clientFactory);

        return builder.Build();
    }

    private static async Task PublishAsync(IHost host, ChunkMessage[] messages)
    {
        using IServiceScope scope = host.Services.CreateScope();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        foreach (ChunkMessage message in messages)
        {
            await publisher.PublishAsync(message).WaitAsync(Timeout);
        }
    }

    // Registers the handler before connecting, since the topics are subscribed right after the connection
    private static Task WaitUntilSubscribedAsync(MqttConsumer consumer)
    {
        TaskCompletionSource<bool> subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.Client.Subscribed.AddHandler(_ =>
        {
            subscribed.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        return subscribed.Task;
    }

    // Every chunk delivered to the consumer (as many as the observer received) was acknowledged exactly once
    private static bool AreAllChunksAcknowledged(MqttClientRecorder recorder, int observedChunksCount)
    {
        ushort[] delivered = [.. recorder.GetDeliveries().Select(delivery => delivery.PacketIdentifier).Order()];
        ushort[] acknowledged = [.. recorder.GetAcknowledgments().Select(acknowledgment => acknowledgment.PacketIdentifier).Order()];

        return delivered.Length == observedChunksCount && delivered.SequenceEqual(acknowledged);
    }

    // Returns the maximum number of chunks of a message
    private static int VerifyContiguousChunks(ObservedChunk[] chunks)
    {
        int index = 0;
        int maxChunksCount = 0;

        while (index < chunks.Length)
        {
            string? chunkMessageId = chunks[index].ChunkMessageId;
            ObservedChunk[] messageChunks = [.. chunks.Skip(index).TakeWhile(chunk => chunk.ChunkMessageId == chunkMessageId)];

            messageChunks.Length.ShouldBeGreaterThan(1, $"The message {chunkMessageId} wasn't split");
            messageChunks.Select(chunk => chunk.ChunkIndex)
                .ShouldBe(Enumerable.Range(0, messageChunks.Length).Select(chunkIndex => chunkIndex.ToString(CultureInfo.InvariantCulture)));
            messageChunks.Select(chunk => chunk.IsLastChunk).ShouldBe(messageChunks.Select((_, chunkIndex) => chunkIndex == messageChunks.Length - 1));

            maxChunksCount = Math.Max(maxChunksCount, messageChunks.Length);
            index += messageChunks.Length;
        }

        return maxChunksCount;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition())
        {
            stopwatch.Elapsed.ShouldBeLessThan(Timeout, $"Timed out waiting for {description}");
            await Task.Delay(50);
        }
    }

    private sealed record ObservedChunk(int PayloadLength, string? ChunkMessageId, string? ChunkIndex, bool IsLastChunk)
    {
        public static ObservedChunk Create(MqttApplicationMessage message) =>
            new(
                (int)message.Payload.Length,
                GetUserProperty(message, MqttMessageHeaders.ChunkMessageId),
                GetUserProperty(message, DefaultMessageHeaders.ChunkIndex),
                GetUserProperty(message, DefaultMessageHeaders.IsLastChunk) == bool.TrueString);

        public override string ToString() => $"{ChunkMessageId}#{ChunkIndex}{(IsLastChunk ? " (last)" : string.Empty)}: {PayloadLength} bytes";

        private static string? GetUserProperty(MqttApplicationMessage message, string name) =>
            message.UserProperties?.FirstOrDefault(property => property.Name == name)?.ReadValueAsString();
    }

    private sealed record ChunkMessage
    {
        public int Number { get; init; }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Serialized")]
        public string Content { get; init; } = string.Empty;
    }
}
