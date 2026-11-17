// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWhileBinaryMessageNotFullyReadInBatch(bool isPartlyRead)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;
        int abortedBatchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
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
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            try
            {
                await foreach (BinaryMessage message in batch)
                {
                    // Move on to the next message of the first batch without fully reading the second binary message: the push of its
                    // next chunk is pending, until the batch is aborted by the disconnect
                    if (isFirstBatch && list.Count == 1)
                    {
                        if (isPartlyRead)
                        {
                            byte[] buffer = new byte[5];
                            await message.Content!.ReadExactlyAsync(buffer);
                        }

                        list.Add(null);
                        continue;
                    }

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

        for (int i = 0; i < chunks2.Length; i++)
        {
            await producer.RawProduceAsync(chunks2[i], HeadersHelper.GetChunkHeaders("2", i, chunks2.Length));
        }

        await AsyncTestingUtil.WaitAsync(() => batches is [{ Count: 2 }]);

        // The disconnect must not wait forever for the pending chunk push
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        abortedBatchesCount.ShouldBe(1);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();

        // The aborted batch is consumed again after reconnecting
        await consumer.Client.ConnectAsync();
        await producer.RawProduceAsync("Message 3"u8.ToArray(), HeadersHelper.GetChunkHeaders("3", 0, 1));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        abortedBatchesCount.ShouldBe(1);
        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message 1", null]);
        batches[1].ShouldBe(["Long message 1", "Long message 2", "Message 3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWhileNotFullyReadBinaryMessageJoinsBatch()
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        HoldBinaryMessageBeforeSequencerBehavior holdBehavior = new("2");
        int batchesCount = 0;
        int abortedBatchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
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
            .AddSingletonBrokerBehavior(holdBehavior)
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            try
            {
                await foreach (BinaryMessage message in batch)
                {
                    // Move on to the next message of the first batch without reading the second binary message: the push of its next
                    // chunk is pending, until the batch is aborted by the disconnect
                    if (isFirstBatch && list.Count == 1)
                    {
                        list.Add(null);
                        continue;
                    }

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

        for (int i = 0; i < chunks2.Length; i++)
        {
            await producer.RawProduceAsync(chunks2[i], HeadersHelper.GetChunkHeaders("2", i, chunks2.Length));
        }

        // The second binary message is held after its chunk sequence started, before it joins the batch
        await holdBehavior.ReachedTask.WaitAsync(TimeSpan.FromSeconds(10));
        await AsyncTestingUtil.WaitAsync(() => batches is [{ Count: 1 }]);
        batches.ShouldHaveSingleItem().Count.ShouldBe(1);

        // The disconnect cancels the reading and aborts the parents of the pending sequences, then the message joins the batch
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        Task disconnectTask = consumer.Client.DisconnectAsync().AsTask();
        await AsyncTestingUtil.WaitAsync(logEventsCollector.ContainsChannelStopping);
        await Task.Delay(300);
        holdBehavior.Release();

        // The disconnect must not wait forever for the pending chunk push
        await disconnectTask.WaitAsync(TimeSpan.FromSeconds(10));

        abortedBatchesCount.ShouldBe(1);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();

        // The aborted batch is consumed again after reconnecting
        await consumer.Client.ConnectAsync();
        await producer.RawProduceAsync("Message 3"u8.ToArray(), HeadersHelper.GetChunkHeaders("3", 0, 1));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        abortedBatchesCount.ShouldBe(1);
        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message 1", null]);
        batches[1].ShouldBe(["Long message 1", "Long message 2", "Message 3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWhileBinaryMessageStartingBatchNotFullyRead(bool isPartlyRead)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;
        int abortedBatchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
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
                        .EnableBatchProcessing(2))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            try
            {
                await foreach (BinaryMessage message in batch)
                {
                    // Move on to the next message of the first batch without fully reading the binary message that started it (sharing
                    // its context with the batch): the push of its next chunk is pending, until the batch is aborted by the disconnect
                    if (isFirstBatch)
                    {
                        if (isPartlyRead)
                        {
                            byte[] buffer = new byte[5];
                            await message.Content!.ReadExactlyAsync(buffer);
                        }

                        list.Add(null);
                        continue;
                    }

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

        for (int i = 0; i < chunks1.Length; i++)
        {
            await producer.RawProduceAsync(chunks1[i], HeadersHelper.GetChunkHeaders("1", i, chunks1.Length));
        }

        await AsyncTestingUtil.WaitAsync(() => batches is [{ Count: 1 }]);
        batches.Count.ShouldBe(1);
        batches[0].Count.ShouldBe(1);

        // The disconnect must not wait forever for the pending chunk push
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        abortedBatchesCount.ShouldBe(1);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();

        // The aborted batch is consumed again after reconnecting
        await consumer.Client.ConnectAsync();
        await producer.RawProduceAsync("Message 2"u8.ToArray(), HeadersHelper.GetChunkHeaders("2", 0, 1));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        abortedBatchesCount.ShouldBe(1);
        batches.Count.ShouldBe(2);
        batches[0].ShouldBe([null]);
        batches[1].ShouldBe(["Long message 1", "Message 2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Chunking_ShouldAbortAndConsumeAgain_WhenPartitionRevokedWhileBinaryMessageNotFullyReadInBatch(bool isPartlyRead)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;
        int abortedBatchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
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
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            try
            {
                await foreach (BinaryMessage message in batch)
                {
                    // Move on to the next message of the first batch without fully reading the second binary message: the push of its
                    // next chunk is pending, until the batch is aborted by the partition revocation
                    if (isFirstBatch && list.Count == 1)
                    {
                        if (isPartlyRead)
                        {
                            byte[] buffer = new byte[5];
                            await message.Content!.ReadExactlyAsync(buffer);
                        }

                        list.Add(null);
                        continue;
                    }

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

        for (int i = 0; i < chunks2.Length; i++)
        {
            await producer.RawProduceAsync(chunks2[i], HeadersHelper.GetChunkHeaders("2", i, chunks2.Length));
        }

        await AsyncTestingUtil.WaitAsync(() => batches is [{ Count: 2 }]);
        batches.Count.ShouldBe(1);
        batches[0].Count.ShouldBe(2);

        // The revocation, stopping the partition channel within the rebalance callback, must not wait forever for the pending chunk push
        DefaultConsumerGroup.Rebalance();
        await AsyncTestingUtil.WaitAsync(() =>
            abortedBatchesCount > 0 && logEventsCollector.Contains(KafkaLogEvents.PartitionRevoked.EventId, $"{DefaultTopicName}[0]"));

        abortedBatchesCount.ShouldBe(1);
        logEventsCollector.Contains(KafkaLogEvents.PartitionRevoked.EventId, $"{DefaultTopicName}[0]").ShouldBeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // The aborted batch is consumed again after the partition is assigned again
        await producer.RawProduceAsync("Message 3"u8.ToArray(), HeadersHelper.GetChunkHeaders("3", 0, 1));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        abortedBatchesCount.ShouldBe(1);
        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message 1", null]);
        batches[1].ShouldBe(["Long message 1", "Long message 2", "Message 3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task Chunking_ShouldCommitAndSkipRemainingChunks_WhenBatchCompletesWhileBinaryMessageNotFullyRead(
        bool isPartlyRead,
        bool isCompletedByMaxWaitTime)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
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
                        .EnableBatchProcessing(
                            isCompletedByMaxWaitTime ? 10 : 2,
                            isCompletedByMaxWaitTime ? TimeSpan.FromMilliseconds(500) : null))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            await foreach (BinaryMessage message in batch)
            {
                // Move on without fully reading the second binary message of the first batch: the push of its next chunk is still
                // pending when the batch completes (because of its size or its max wait time)
                if (isFirstBatch && list.Count == 1)
                {
                    if (isPartlyRead)
                    {
                        byte[] buffer = new byte[5];
                        await message.Content!.ReadExactlyAsync(buffer);
                    }

                    list.Add(null);
                    continue;
                }

                byte[]? readAll = await message.Content.ReadAllAsync();
                list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks1 = [.. "Long message 1"u8.ToArray().Chunk(5)];
        byte[][] chunks2 = [.. "Long message 2"u8.ToArray().Chunk(5)];

        for (int i = 0; i < chunks1.Length; i++)
        {
            await producer.RawProduceAsync(chunks1[i], HeadersHelper.GetChunkHeaders("1", i, chunks1.Length));
        }

        for (int i = 0; i < chunks2.Length; i++)
        {
            await producer.RawProduceAsync(chunks2[i], HeadersHelper.GetChunkHeaders("2", i, chunks2.Length));
        }

        await producer.RawProduceAsync("Message 3"u8.ToArray(), HeadersHelper.GetChunkHeaders("3", 0, 1));
        await producer.RawProduceAsync("Message 4"u8.ToArray(), HeadersHelper.GetChunkHeaders("4", 0, 1));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);

        // The first batch is committed when it completes, and the remaining chunks of the binary message are skipped
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 8);

        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message 1", null]);
        batches[1].ShouldBe(["Message 3", "Message 4"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(8);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Chunking_ShouldCommitAndSkipRemainingChunks_WhenBatchStartedByBinaryMessageCompletesWhileNotFullyRead(bool isPartlyRead)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
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
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500)))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            await foreach (BinaryMessage message in batch)
            {
                // Move on without fully reading the binary message starting the first batch: the push of its next chunk is still pending
                // when the batch completes because of its max wait time
                if (isFirstBatch)
                {
                    if (isPartlyRead)
                    {
                        byte[] buffer = new byte[5];
                        await message.Content!.ReadExactlyAsync(buffer);
                    }

                    list.Add(null);
                    continue;
                }

                byte[]? readAll = await message.Content.ReadAllAsync();
                list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks1 = [.. "Long message 1"u8.ToArray().Chunk(5)];

        for (int i = 0; i < chunks1.Length; i++)
        {
            await producer.RawProduceAsync(chunks1[i], HeadersHelper.GetChunkHeaders("1", i, chunks1.Length));
        }

        await producer.RawProduceAsync("Message 2"u8.ToArray(), HeadersHelper.GetChunkHeaders("2", 0, 1));
        await producer.RawProduceAsync("Message 3"u8.ToArray(), HeadersHelper.GetChunkHeaders("3", 0, 1));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);

        // The first batch is committed when it completes, and the remaining chunks of the binary message are skipped
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 5);

        batches.Count.ShouldBe(2);
        batches[0].ShouldBe([null]);
        batches[1].ShouldBe(["Message 2", "Message 3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("next chunked message")]
    public async Task Chunking_ShouldNotMoveCommittedOffsetBack_WhenIncompleteMessageIsAbortedAfterBatchIsCommitted(string abortTrigger)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithSequenceTimeout(TimeSpan.FromSeconds(abortTrigger == "timeout" ? 1 : 600))
                        .EnableBatchProcessing(2))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }

            batches.Add(list);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks1 = ChunksHelper.SerializeAndSplitIntoChunks("Long message 1", 3);
        byte[][] chunks2 = ChunksHelper.SerializeAndSplitIntoChunks("Long message 2", 3);

        // The first chunk of a message that is never completed, followed by a batch that is committed while the chunked message is pending
        // (the batch doesn't wait for the chunk sequence, which is aborted only by its timeout or by the next chunked message)
        await producer.RawProduceAsync(chunks1[0], HeadersHelper.GetChunkHeaders("1", 0, chunks1.Length, typeof(TestEventOne)));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        batches.ShouldBe([["1", "2"]]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);

        if (abortTrigger != "timeout")
            await producer.RawProduceAsync(chunks2[0], HeadersHelper.GetChunkHeaders("2", 0, chunks2.Length, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(() => logEventsCollector.Count(IntegrationLogEvents.IncompleteSequenceAborted.EventId) > 0);

        // The aborted chunked message is skipped without moving the committed offset back before the batch
        logEventsCollector.Count(IntegrationLogEvents.IncompleteSequenceAborted.EventId).ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "4" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        // The batch isn't consumed again after reconnecting
        batches.ShouldBe([["1", "2"], ["3", "4"]]);
    }

    // Holds the first consumed binary message with the specified key right before it's added to the batch, after its chunk sequence was
    // started by the raw sequencer
    private sealed class HoldBinaryMessageBeforeSequencerBehavior : IConsumerBehavior
    {
        private readonly string _messageKey;

        private readonly TaskCompletionSource _reachedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _releaseTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public HoldBinaryMessageBeforeSequencerBehavior(string messageKey)
        {
            _messageKey = messageKey;
        }

        public Task ReachedTask => _reachedTaskCompletionSource.Task;

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Sequencer - 1;

        public void Release() => _releaseTaskCompletionSource.TrySetResult();

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            if (context.Envelope.Headers.GetValue(KafkaMessageHeaders.MessageKey) == _messageKey &&
                _reachedTaskCompletionSource.TrySetResult())
            {
                await _releaseTaskCompletionSource.Task;
            }

            await next(context, cancellationToken);
        }
    }
}
