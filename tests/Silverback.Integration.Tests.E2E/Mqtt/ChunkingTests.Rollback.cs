// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ChunkingTests
{
    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeChunkedJsonWithCustomHeaderNames()
    {
        const int chunksPerMessage = 4;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .WithCustomHeaderName(DefaultMessageHeaders.ChunkIndex, "x-quack-index")
            .WithCustomHeaderName(DefaultMessageHeaders.ChunksCount, "x-quack-count")
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS())))
            .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(5 * chunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.ShouldBeLessThanOrEqualTo(ChunkSize));

        // The chunk headers are renamed, while the chunked message id (which can't be renamed) is still sent to correlate the chunks
        IReadOnlyList<MqttApplicationMessage> messages = GetDefaultTopicMessages();
        messages.Count.ShouldBe(5 * chunksPerMessage);

        for (int i = 0; i < messages.Count; i++)
        {
            MqttApplicationMessage message = messages[i];

            GetUserProperty(message, DefaultMessageHeaders.ChunkIndex).ShouldBeNull();
            GetUserProperty(message, DefaultMessageHeaders.ChunksCount).ShouldBeNull();
            GetUserProperty(message, "x-quack-index").ShouldBe((i % chunksPerMessage).ToString(CultureInfo.InvariantCulture));
            GetUserProperty(message, "x-quack-count").ShouldBe(chunksPerMessage.ToString(CultureInfo.InvariantCulture));
            GetUserProperty(message, MqttMessageHeaders.ChunkMessageId).ShouldNotBeNullOrEmpty();
        }

        messages.Chunk(chunksPerMessage)
            .Select(chunks => chunks
                .Select(chunk => GetUserProperty(chunk, MqttMessageHeaders.ChunkMessageId))
                .Distinct()
                .ShouldHaveSingleItem())
            .Distinct()
            .Count()
            .ShouldBe(5);

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldReplayCompleteChunkedMessage_WhenBatchIsRetriedWhileNextChunkedMessageIsIncomplete()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TaskCompletionSource<bool> firstMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> failBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(2, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            if (Interlocked.Increment(ref batchesCount) > 1)
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }

                return;
            }

            using CancellationTokenSource cancellationTokenSource = new();
            await using IAsyncEnumerator<TestEventOne> enumerator = batch.GetAsyncEnumerator(cancellationTokenSource.Token);

            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            list.Add(enumerator.Current.ContentEventOne);

            // Pull the next message, letting the consumer move past the first chunked message and receive the chunks of the next one
            ValueTask<bool> moveNextTask = enumerator.MoveNextAsync();
            firstMessageReceived.TrySetResult(true);

            await failBatch.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await cancellationTokenSource.CancelAsync();

            try
            {
                await moveNextTask;
            }
            catch (OperationCanceledException)
            {
                // Expected: the next chunked message is incomplete and the enumeration is canceled
            }

            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunksA = SerializeAndSplitIntoChunks("Long message A", 3);
        byte[][] chunksB = SerializeAndSplitIntoChunks("Long message B", 3);

        for (int i = 0; i < chunksA.Length; i++)
        {
            await producer.RawProduceAsync(chunksA[i], GetRawChunkHeaders("A", i, chunksA.Length));
        }

        await firstMessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await producer.RawProduceAsync(chunksB[0], GetRawChunkHeaders("B", 0, chunksB.Length));
        await producer.RawProduceAsync(chunksB[1], GetRawChunkHeaders("B", 1, chunksB.Length));

        // Once the second chunk is being processed, the consumer is done with the first one, which started the chunk sequence:
        // fail the batch while the chunk sequence is pending, so that only the chunk sequence is rolled back
        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Any(envelope => IsRawChunk(envelope, "B", 1)));
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "B", 1)).ShouldBe(1);
        failBatch.TrySetResult(true);

        // The complete chunked message is replayed into a new batch, which waits for the last chunk of the incomplete one
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count == 2 && receivedBatches[1].Count == 1);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["Long message A"]);
        receivedBatches[1].ShouldBe(["Long message A"]);

        await producer.RawProduceAsync(chunksB[2], GetRawChunkHeaders("B", 2, chunksB.Length));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["Long message A"]);
        receivedBatches[1].ShouldBe(["Long message A", "Long message B"]);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "A", 0)).ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "B", 0)).ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "B", 2)).ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldRetryBatchConfiguredTimes_WhenFirstMessageIsChunked()
    {
        int tryCount = 0;
        List<string?>? processedMessages = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(2, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddIntegrationSpy());

        // Each failure must count as a single attempt, although the batch is started by a chunked message
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            int currentTry = Interlocked.Increment(ref tryCount);
            List<string?> messages = [];

            await foreach (TestEventOne message in batch)
            {
                messages.Add(message.ContentEventOne);
            }

            if (currentTry <= 2)
                throw new InvalidOperationException("Retry!");

            processedMessages = messages;
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(3);
        processedMessages.ShouldBe(["Long message 1", "Long message 2"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldReplayBatchFromFirstMessage_WhenBatchIsRetriedWhileChunkedMessageIsIncomplete()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);

                // Fail the first attempt while the chunked message received after the first message is incomplete
                if (message.ContentEventOne == "2" && Interlocked.Increment(ref tryCount) == 1)
                    throw new InvalidOperationException("Retry!");
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = SerializeAndSplitIntoChunks("Long message", 2);

        // The last chunk is read only after the failure, since the consumer waits for the batch to pull the message "2"
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.RawProduceAsync(chunks[0], GetRawChunkHeaders("C", 0, chunks.Length));
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.RawProduceAsync(chunks[1], GetRawChunkHeaders("C", 1, chunks.Length));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(2);
        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["1", "2"]);
        receivedBatches[1].ShouldBe(["1", "2", "Long message"]);

        // Both plain messages are replayed, as well as the first chunk, while the last chunk is read once
        Helper.Spy.RawInboundEnvelopes.Count(envelope => envelope.Headers.GetValue(MqttMessageHeaders.ChunkMessageId) == null)
            .ShouldBe(4);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "C", 0)).ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "C", 1)).ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    private static byte[][] SerializeAndSplitIntoChunks(string content, int chunksCount)
    {
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = content });
        int chunkSize = (rawMessage.Length + chunksCount - 1) / chunksCount;

        return [.. rawMessage.Chunk(chunkSize)];
    }

    private static MessageHeader[] GetRawChunkHeaders(string chunkMessageId, int chunkIndex, int chunksCount) =>
    [
        new(MqttMessageHeaders.ChunkMessageId, chunkMessageId),
        new(DefaultMessageHeaders.ChunkIndex, chunkIndex.ToString(CultureInfo.InvariantCulture)),
        new(DefaultMessageHeaders.ChunksCount, chunksCount.ToString(CultureInfo.InvariantCulture))
    ];

    private static bool IsRawChunk(IRawInboundEnvelope envelope, string chunkMessageId, int chunkIndex) =>
        envelope.Headers.GetValue(MqttMessageHeaders.ChunkMessageId) == chunkMessageId &&
        envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex) == chunkIndex.ToString(CultureInfo.InvariantCulture);
}
