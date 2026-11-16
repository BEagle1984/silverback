// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingTests
{
    [Fact]
    public async Task Chunking_ShouldConsumeJsonAndCommit_WhenConsumedInBatch()
    {
        const int messagesCount = 9;
        const int chunkSize = 10;
        const int chunksPerMessage = 4;
        const int batchSize = 3;

        TestingCollection<List<TestEventOne>> batches = [];
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(batchSize))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> streamEnumerable)
        {
            List<TestEventOne> list = [];
            batches.Add(list);

            await foreach (TestEventOne message in streamEnumerable)
            {
                list.Add(message);

                long actualCommittedOffsets = DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                int expectedCommittedOffsets = chunksPerMessage * batchSize * (batches.Count - 1);

                if (actualCommittedOffsets != expectedCommittedOffsets)
                {
                    failedCommit ??=
                        $"{actualCommittedOffsets} != {expectedCommittedOffsets} " +
                        $"({batches.Count}.{list.Count})";
                }
            }

            if (list.Count != 3)
            {
                enumerationAborted ??=
                    $"Enumeration completed after {list.Count} messages " +
                    $"({batches.Count}.{list.Count})";
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(messagesCount * chunksPerMessage);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(messagesCount * chunksPerMessage);

        failedCommit.ShouldBeNull();
        enumerationAborted.ShouldBeNull();

        batches.Count.ShouldBe(messagesCount / batchSize);
        batches[0].Count.ShouldBe(batchSize);
        batches[1].Count.ShouldBe(batchSize);
        batches[2].Count.ShouldBe(batchSize);

        batches[0][0].ContentEventOne.ShouldBe("Long message 1");
        batches[0][1].ContentEventOne.ShouldBe("Long message 2");
        batches[0][2].ContentEventOne.ShouldBe("Long message 3");

        batches[1][0].ContentEventOne.ShouldBe("Long message 4");
        batches[1][1].ContentEventOne.ShouldBe("Long message 5");
        batches[1][2].ContentEventOne.ShouldBe("Long message 6");

        batches[2][0].ContentEventOne.ShouldBe("Long message 7");
        batches[2][1].ContentEventOne.ShouldBe("Long message 8");
        batches[2][2].ContentEventOne.ShouldBe("Long message 9");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeBinaryMessageAndCommit_WhenConsumedInBatch()
    {
        const int messagesCount = 15;
        const int chunkSize = 5;
        const int chunksPerMessage = 3;
        const int batchSize = 5;

        TestingCollection<List<string?>> batches = [];
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(batchSize))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatch)
            .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<BinaryMessage> streamEnumerable)
        {
            List<string?> list = [];
            batches.Add(list);

            await foreach (BinaryMessage message in streamEnumerable)
            {
                long actualCommittedOffsets = DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                int expectedCommittedOffsets = chunksPerMessage * batchSize * (batches.Count - 1);

                if (actualCommittedOffsets != expectedCommittedOffsets)
                {
                    failedCommit ??=
                        $"{actualCommittedOffsets} != {expectedCommittedOffsets} " +
                        $"({batches.Count}.{list.Count})";
                }

                byte[]? readAll = await message.Content.ReadAllAsync();
                list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
            }

            if (list.Count != 5)
            {
                enumerationAborted ??=
                    $"Enumeration completed after {list.Count} messages " +
                    $"({batches.Count}.{list.Count})";
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new BinaryMessage(Encoding.UTF8.GetBytes($"Long message {i}")));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        failedCommit.ShouldBeNull();
        enumerationAborted.ShouldBeNull();

        batches.Count.ShouldBe(messagesCount / batchSize);
        batches[0].Count.ShouldBe(batchSize);
        batches[1].Count.ShouldBe(batchSize);
        batches[2].Count.ShouldBe(batchSize);

        batches[0][0].ShouldBe("Long message 1");
        batches[0][1].ShouldBe("Long message 2");
        batches[0][2].ShouldBe("Long message 3");
        batches[0][3].ShouldBe("Long message 4");
        batches[0][4].ShouldBe("Long message 5");

        batches[1][0].ShouldBe("Long message 6");
        batches[1][1].ShouldBe("Long message 7");
        batches[1][2].ShouldBe("Long message 8");
        batches[1][3].ShouldBe("Long message 9");
        batches[1][4].ShouldBe("Long message 10");

        batches[2][0].ShouldBe("Long message 11");
        batches[2][1].ShouldBe("Long message 12");
        batches[2][2].ShouldBe("Long message 13");
        batches[2][3].ShouldBe("Long message 14");
        batches[2][4].ShouldBe("Long message 15");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeSingleChunkJsonInBatch()
    {
        const int messagesCount = 9;
        const int chunkSize = 100;
        const int chunksPerMessage = 1;
        const int batchSize = 3;

        TestingCollection<List<TestEventOne>> batches = [];
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(batchSize))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
            .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> streamEnumerable)
        {
            List<TestEventOne> list = [];
            batches.Add(list);

            await foreach (TestEventOne message in streamEnumerable)
            {
                list.Add(message);

                long actualCommittedOffsets = DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                int expectedCommittedOffsets = chunksPerMessage * batchSize * (batches.Count - 1);

                if (actualCommittedOffsets != expectedCommittedOffsets)
                {
                    failedCommit ??=
                        $"{actualCommittedOffsets} != {expectedCommittedOffsets} " +
                        $"({batches.Count}.{list.Count})";
                }
            }

            if (list.Count != 3)
            {
                enumerationAborted ??=
                    $"Enumeration completed after {list.Count} messages " +
                    $"({batches.Count}.{list.Count})";
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(messagesCount * chunksPerMessage);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(messagesCount * chunksPerMessage);

        failedCommit.ShouldBeNull();
        enumerationAborted.ShouldBeNull();

        batches.Count.ShouldBe(messagesCount / batchSize);
        batches[0].Count.ShouldBe(batchSize);
        batches[1].Count.ShouldBe(batchSize);
        batches[2].Count.ShouldBe(batchSize);

        batches[0][0].ContentEventOne.ShouldBe("Long message 1");
        batches[0][1].ContentEventOne.ShouldBe("Long message 2");
        batches[0][2].ContentEventOne.ShouldBe("Long message 3");

        batches[1][0].ContentEventOne.ShouldBe("Long message 4");
        batches[1][1].ContentEventOne.ShouldBe("Long message 5");
        batches[1][2].ContentEventOne.ShouldBe("Long message 6");

        batches[2][0].ContentEventOne.ShouldBe("Long message 7");
        batches[2][1].ContentEventOne.ShouldBe("Long message 8");
        batches[2][2].ContentEventOne.ShouldBe("Long message 9");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeSingleChunkBinaryMessageInBatch()
    {
        const int messagesCount = 15;
        const int chunkSize = 50;
        const int chunksPerMessage = 1;
        const int batchSize = 5;

        TestingCollection<List<string?>> batches = [];
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(batchSize))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatch)
            .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<BinaryMessage> streamEnumerable)
        {
            List<string?> list = [];
            batches.Add(list);

            await foreach (BinaryMessage message in streamEnumerable)
            {
                long actualCommittedOffsets = DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                int expectedCommittedOffsets = chunksPerMessage * batchSize * (batches.Count - 1);

                if (actualCommittedOffsets != expectedCommittedOffsets)
                {
                    failedCommit ??=
                        $"{actualCommittedOffsets} != {expectedCommittedOffsets} " +
                        $"({batches.Count}.{list.Count})";
                }

                byte[]? readAll = await message.Content.ReadAllAsync();
                list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
            }

            if (list.Count != 5)
            {
                enumerationAborted ??=
                    $"Enumeration completed after {list.Count} messages " +
                    $"({batches.Count}.{list.Count})";
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new BinaryMessage(Encoding.UTF8.GetBytes($"Long message {i}")));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        failedCommit.ShouldBeNull();
        enumerationAborted.ShouldBeNull();

        batches.Count.ShouldBe(messagesCount / batchSize);
        batches[0].Count.ShouldBe(batchSize);
        batches[1].Count.ShouldBe(batchSize);
        batches[2].Count.ShouldBe(batchSize);

        batches[0][0].ShouldBe("Long message 1");
        batches[0][1].ShouldBe("Long message 2");
        batches[0][2].ShouldBe("Long message 3");
        batches[0][3].ShouldBe("Long message 4");
        batches[0][4].ShouldBe("Long message 5");

        batches[1][0].ShouldBe("Long message 6");
        batches[1][1].ShouldBe("Long message 7");
        batches[1][2].ShouldBe("Long message 8");
        batches[1][3].ShouldBe("Long message 9");
        batches[1][4].ShouldBe("Long message 10");

        batches[2][0].ShouldBe("Long message 11");
        batches[2][1].ShouldBe("Long message 12");
        batches[2][2].ShouldBe("Long message 13");
        batches[2][3].ShouldBe("Long message 14");
        batches[2][4].ShouldBe("Long message 15");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWhileJsonConsumedInBatch()
    {
        TestingCollection<List<string?>> batches = [];
        int abortedBatchesCount = 0;

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
                        .EnableBatchProcessing(3))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            batches.Add(list);

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref abortedBatchesCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunksA = ChunksHelper.SerializeAndSplitIntoChunks("Long message A", 3);
        byte[][] chunksB = ChunksHelper.SerializeAndSplitIntoChunks("Long message B", 3);

        for (int i = 0; i < chunksA.Length; i++)
        {
            await producer.RawProduceAsync(chunksA[i], HeadersHelper.GetChunkHeaders("A", i, chunksA.Length, typeof(TestEventOne)));
        }

        await producer.RawProduceAsync(chunksB[0], HeadersHelper.GetChunkHeaders("B", 0, chunksB.Length, typeof(TestEventOne)));
        await producer.RawProduceAsync(chunksB[1], HeadersHelper.GetChunkHeaders("B", 1, chunksB.Length, typeof(TestEventOne)));

        // Disconnect while the batch and the next chunked message are pending
        await AsyncTestingUtil.WaitAsync(() => batches is [{ Count: 1 }] && Helper.Spy.RawInboundEnvelopes.Count == 5);
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedBatchesCount > 0);

        abortedBatchesCount.ShouldBe(1);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // The aborted batch is consumed again after reconnecting
        await consumer.Client.ConnectAsync();
        await producer.RawProduceAsync(chunksB[2], HeadersHelper.GetChunkHeaders("B", 2, chunksB.Length, typeof(TestEventOne)));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        abortedBatchesCount.ShouldBe(1);
        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message A"]);
        batches[1].ShouldBe(["Long message A", "Long message B", "3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWhileBinaryMessageConsumedInBatch()
    {
        TestingCollection<List<string?>> batches = [];
        int abortedBatchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<BinaryMessage>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            List<string?> list = [];
            batches.Add(list);

            try
            {
                await foreach (BinaryMessage message in batch)
                {
                    byte[]? readAll = await message.Content.ReadAllAsync();
                    list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref abortedBatchesCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks1 = [.. "Long message 1"u8.ToArray().Chunk(5)];
        byte[][] chunks2 = [.. "Long message 2"u8.ToArray().Chunk(5)];

        for (int i = 0; i < chunks1.Length; i++)
        {
            await producer.RawProduceAsync(chunks1[i], HeadersHelper.GetChunkHeaders("1", i, chunks1.Length));
        }

        await producer.RawProduceAsync(chunks2[0], HeadersHelper.GetChunkHeaders("2", 0, chunks2.Length));
        await producer.RawProduceAsync(chunks2[1], HeadersHelper.GetChunkHeaders("2", 1, chunks2.Length));

        // Disconnect while the subscriber is reading the incomplete binary message added to the batch
        await AsyncTestingUtil.WaitAsync(() => batches is [{ Count: 1 }] && Helper.Spy.RawInboundEnvelopes.Count == 5);
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => abortedBatchesCount > 0);

        abortedBatchesCount.ShouldBe(1);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // The aborted batch is consumed again after reconnecting
        await consumer.Client.ConnectAsync();
        await producer.RawProduceAsync(chunks2[2], HeadersHelper.GetChunkHeaders("2", 2, chunks2.Length));
        await producer.RawProduceAsync("Message 3"u8.ToArray(), HeadersHelper.GetChunkHeaders("3", 0, 1));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        abortedBatchesCount.ShouldBe(1);
        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message 1"]);
        batches[1].ShouldBe(["Long message 1", "Long message 2", "Message 3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
    }
}
