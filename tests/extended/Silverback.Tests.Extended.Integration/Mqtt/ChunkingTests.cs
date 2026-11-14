// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Messaging.Messages;
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
public class ChunkingTests : BrokerHarnessTests<MqttTestHarness>
{
    private const int ChunkSize = 50;

    private const int MessagesCount = 12;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public ChunkingTests(MqttFixture fixture, ITestOutputHelper output)
        : base(new MqttTestHarness("chunk", output))
    {
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Chunking_ShouldReassembleAndAcknowledgeChunkedMessages(int maxDegreeOfParallelism)
    {
        string topic = Harness.Prefix;
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

        // Created before the host, so that it records the deliveries before Silverback handles them
        MqttClientRecorder recorder = new(Harness.NativeClient);

        // The consumer is registered first, to get the native client of the harness that the recorder observes. A dedicated client
        // consumes the chunked messages, so that the chunks of each message are contiguous.
        Harness.AddClient(client => client
            .RequestPersistentSession(TimeSpan.FromMinutes(5))
            .EnableParallelProcessing(maxDegreeOfParallelism)
            .Consume<ChunkMessage>(endpoint => endpoint
                .ConsumeFrom(topic)
                .WithAtLeastOnceQoS()
                .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())));
        Harness.AddClient(Harness.Prefix + "-producer", client => client
            .Produce<ChunkMessage>(endpoint => endpoint
                .ProduceTo(topic)
                .WithAtLeastOnceQoS()
                .EnableChunking(ChunkSize)));
        Harness.ConfigureSilverback(silverback => silverback.AddDelegateSubscriber<ChunkMessage>(consumed.Enqueue));
        Harness.OnStopped(() =>
        {
            Harness.Output.WriteLine($"Client={Harness.Prefix}; consumed: {string.Join(", ", consumed.Select(message => message.Number))}");
            Harness.Output.WriteLine($"Chunks: {string.Join(", ", chunks)}");
            return Task.CompletedTask;
        });

        await Harness.InitializeAsync();
        await Harness.ConnectAsync();

        // Guards the registration order, which a wrong one would only show as a misleading acknowledgment timeout
        Harness.NativeClient.Options.ClientId.ShouldBe(Harness.Prefix);

        // An independent client observes the chunks produced to the broker
        TestMqttClient observer = await Harness.CreateClientAsync("observer");
        observer.Native.ApplicationMessageReceivedAsync += args =>
        {
            chunks.Enqueue(ObservedChunk.Create(args.ApplicationMessage));

            return Task.CompletedTask;
        };
        await observer.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);

        // Produced while the consumer is disconnected, so that the broker delivers the chunks of several messages at once (up to its
        // in-flight limit) when the session is resumed
        await Harness.DisconnectConsumerAsync();
        await PublishAsync(messages);
        await Harness.WaitUntilAsync(() => chunks.Count(chunk => chunk.IsLastChunk) == MessagesCount, "the chunks to be produced");

        await Harness.ReconnectAsync();

        await Harness.WaitUntilAsync(() => consumed.Count >= MessagesCount, "the chunked messages to be consumed");

        // Each chunk is acknowledged once its message is processed
        await Harness.WaitUntilAsync(() => AreAllChunksAcknowledged(recorder, chunks.Count), "the chunks to be acknowledged");

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
        Harness.Logs.Errors.ShouldBeEmpty();

        // The broker doesn't replay any chunk when the session is resumed
        await Harness.DisconnectConsumerAsync();
        SessionReplay replay = await Harness.ResumeAndEndSessionAsync(topic);

        replay.IsSessionPresent.ShouldBeTrue();
        replay.Payloads.ShouldBeEmpty();
        replay.Releases.ShouldBe(0);
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

    private async Task PublishAsync(ChunkMessage[] messages)
    {
        using IServiceScope scope = Harness.Services.CreateScope();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        foreach (ChunkMessage message in messages)
        {
            await publisher.PublishAsync(message).WaitAsync(Harness.Timeout);
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
