// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingTests
{
    [Fact]
    public async Task Chunking_ShouldReplayBatch_WhenBatchIsRetriedWhileNextChunkedMessageIsIncomplete()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TaskCompletionSource<bool> firstMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> failBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2)
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
            await producer.RawProduceAsync(chunksA[i], HeadersHelper.GetChunkHeaders("A", i, chunksA.Length, typeof(TestEventOne)));
        }

        await firstMessageReceived.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await producer.RawProduceAsync(chunksB[0], HeadersHelper.GetChunkHeaders("B", 0, chunksB.Length, typeof(TestEventOne)));
        await producer.RawProduceAsync(chunksB[1], HeadersHelper.GetChunkHeaders("B", 1, chunksB.Length, typeof(TestEventOne)));

        // Fail the batch while the next chunked message is pending
        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Any(envelope => IsRawChunk(envelope, "B", 1)));
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "B", 1)).ShouldBe(1);
        failBatch.TrySetResult(true);

        // The failed batch is consumed again, followed by the incomplete chunked message
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count == 2 && receivedBatches[1].Count == 1);
        await producer.RawProduceAsync(chunksB[2], HeadersHelper.GetChunkHeaders("B", 2, chunksB.Length, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["Long message A"]);
        receivedBatches[1].ShouldBe(["Long message A", "Long message B"]);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "A", 0)).ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "B", 0)).ShouldBe(2);
        Helper.Spy.RawInboundEnvelopes.Count(envelope => IsRawChunk(envelope, "B", 2)).ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(6);
    }

    [Fact]
    public async Task Chunking_ShouldRetryMessage_WhenDeserializationFailsWhileBatchAndChunkedMessageArePending()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        int retriesCount = 0;
        int skipsCount = 0;
        IErrorPolicyCallback callback = Substitute.For<IRetryErrorPolicyCallback, ISkipMessageErrorPolicyCallback>();
        ((IRetryErrorPolicyCallback)callback).OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref retriesCount);
                return Task.CompletedTask;
            });
        ((ISkipMessageErrorPolicyCallback)callback).OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref skipsCount);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2)
                        .OnError(policy => policy.Retry(2).ThenSkip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                // The pending batch is aborted by the rollback
                return;
            }

            successfulBatches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = SerializeAndSplitIntoChunks("Long message", 2);

        // A batch and a chunked message are pending when a message fails before the sequencer
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.RawProduceAsync(chunks[0], HeadersHelper.GetChunkHeaders("A", 0, chunks.Length, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);

        await AsyncTestingUtil.WaitAsync(() => skipsCount > 0);
        await producer.RawProduceAsync(chunks[1], HeadersHelper.GetChunkHeaders("A", 1, chunks.Length, typeof(TestEventOne)));
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The retries roll back to the pending batch, instead of reconnecting
        retriesCount.ShouldBe(2);
        skipsCount.ShouldBe(1);
        successfulBatches.ShouldHaveSingleItem().ShouldBe(["1", "Long message"]);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);
    }

    private static byte[][] SerializeAndSplitIntoChunks(string content, int chunksCount)
    {
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = content });
        int chunkSize = (rawMessage.Length + chunksCount - 1) / chunksCount;

        return [.. rawMessage.Chunk(chunkSize)];
    }

    private static bool IsRawChunk(IRawInboundEnvelope envelope, string chunkMessageId, int chunkIndex) =>
        envelope.Headers.GetValue(KafkaMessageHeaders.MessageKey) == chunkMessageId &&
        envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex) == chunkIndex.ToString(CultureInfo.InvariantCulture);
}
