// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingFixture
{
    [Fact]
    public async Task Chunking_ShouldConsumeJsonAndCommit_WhenConsumedInBatch()
    {
        const int messagesCount = 9;
        const int chunkSize = 10;
        const int chunksPerMessage = 4;
        const int batchSize = 3;

        TestingCollection<List<TestEventOne>> batches = new();
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(batchSize))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
                .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> streamEnumerable)
        {
            List<TestEventOne> list = new();
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(messagesCount * chunksPerMessage);
        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(messagesCount * chunksPerMessage);

        failedCommit.Should().BeNull();
        enumerationAborted.Should().BeNull();

        batches.Should().HaveCount(messagesCount / batchSize);
        batches[0].Should().HaveCount(batchSize);
        batches[1].Should().HaveCount(batchSize);
        batches[2].Should().HaveCount(batchSize);

        batches[0][0].ContentEventOne.Should().Be("Long message 1");
        batches[0][1].ContentEventOne.Should().Be("Long message 2");
        batches[0][2].ContentEventOne.Should().Be("Long message 3");

        batches[1][0].ContentEventOne.Should().Be("Long message 4");
        batches[1][1].ContentEventOne.Should().Be("Long message 5");
        batches[1][2].ContentEventOne.Should().Be("Long message 6");

        batches[2][0].ContentEventOne.Should().Be("Long message 7");
        batches[2][1].ContentEventOne.Should().Be("Long message 8");
        batches[2][2].ContentEventOne.Should().Be("Long message 9");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeBinaryMessageAndCommit_WhenConsumedInBatch()
    {
        const int messagesCount = 15;
        const int chunkSize = 5;
        const int chunksPerMessage = 3;
        const int batchSize = 5;

        TestingCollection<List<string?>> batches = new();
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(batchSize))))
                .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatch)
                .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<BinaryMessage> streamEnumerable)
        {
            List<string?> list = new();
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

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new BinaryMessage(Encoding.UTF8.GetBytes($"Long message {i}")));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        failedCommit.Should().BeNull();
        enumerationAborted.Should().BeNull();

        batches.Should().HaveCount(messagesCount / batchSize);
        batches[0].Should().HaveCount(batchSize);
        batches[1].Should().HaveCount(batchSize);
        batches[2].Should().HaveCount(batchSize);

        batches[0][0].Should().Be("Long message 1");
        batches[0][1].Should().Be("Long message 2");
        batches[0][2].Should().Be("Long message 3");
        batches[0][3].Should().Be("Long message 4");
        batches[0][4].Should().Be("Long message 5");

        batches[1][0].Should().Be("Long message 6");
        batches[1][1].Should().Be("Long message 7");
        batches[1][2].Should().Be("Long message 8");
        batches[1][3].Should().Be("Long message 9");
        batches[1][4].Should().Be("Long message 10");

        batches[2][0].Should().Be("Long message 11");
        batches[2][1].Should().Be("Long message 12");
        batches[2][2].Should().Be("Long message 13");
        batches[2][3].Should().Be("Long message 14");
        batches[2][4].Should().Be("Long message 15");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeSingleChunkJsonInBatch()
    {
        const int messagesCount = 9;
        const int chunkSize = 100;
        const int chunksPerMessage = 1;
        const int batchSize = 3;

        TestingCollection<List<TestEventOne>> batches = new();
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(batchSize))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch)
                .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> streamEnumerable)
        {
            List<TestEventOne> list = new();
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(messagesCount * chunksPerMessage);
        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(messagesCount * chunksPerMessage);

        failedCommit.Should().BeNull();
        enumerationAborted.Should().BeNull();

        batches.Should().HaveCount(messagesCount / batchSize);
        batches[0].Should().HaveCount(batchSize);
        batches[1].Should().HaveCount(batchSize);
        batches[2].Should().HaveCount(batchSize);

        batches[0][0].ContentEventOne.Should().Be("Long message 1");
        batches[0][1].ContentEventOne.Should().Be("Long message 2");
        batches[0][2].ContentEventOne.Should().Be("Long message 3");

        batches[1][0].ContentEventOne.Should().Be("Long message 4");
        batches[1][1].ContentEventOne.Should().Be("Long message 5");
        batches[1][2].ContentEventOne.Should().Be("Long message 6");

        batches[2][0].ContentEventOne.Should().Be("Long message 7");
        batches[2][1].ContentEventOne.Should().Be("Long message 8");
        batches[2][2].ContentEventOne.Should().Be("Long message 9");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(chunksPerMessage * messagesCount);
    }

    [Fact]
    public async Task Chunking_SingleChunkBinaryMessageConsumedInBatch_ProducedAndConsumed()
    {
        const int messagesCount = 15;
        const int chunkSize = 50;
        const int chunksPerMessage = 1;
        const int batchSize = 5;

        TestingCollection<List<string?>> batches = new();
        string? failedCommit = null;
        string? enumerationAborted = null;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(batchSize))))
                .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatch)
                .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleBatch(IAsyncEnumerable<BinaryMessage> streamEnumerable)
        {
            List<string?> list = new();
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

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new BinaryMessage(Encoding.UTF8.GetBytes($"Long message {i}")));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        failedCommit.Should().BeNull();
        enumerationAborted.Should().BeNull();

        batches.Should().HaveCount(messagesCount / batchSize);
        batches[0].Should().HaveCount(batchSize);
        batches[1].Should().HaveCount(batchSize);
        batches[2].Should().HaveCount(batchSize);

        batches[0][0].Should().Be("Long message 1");
        batches[0][1].Should().Be("Long message 2");
        batches[0][2].Should().Be("Long message 3");
        batches[0][3].Should().Be("Long message 4");
        batches[0][4].Should().Be("Long message 5");

        batches[1][0].Should().Be("Long message 6");
        batches[1][1].Should().Be("Long message 7");
        batches[1][2].Should().Be("Long message 8");
        batches[1][3].Should().Be("Long message 9");
        batches[1][4].Should().Be("Long message 10");

        batches[2][0].Should().Be("Long message 11");
        batches[2][1].Should().Be("Long message 12");
        batches[2][2].Should().Be("Long message 13");
        batches[2][3].Should().Be("Long message 14");
        batches[2][4].Should().Be("Long message 15");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(chunksPerMessage * messagesCount);
    }

    [Fact(Skip = "Not yet implemented")]
    public Task Chunking_ShouldAbortSequence_WhenDisconnectingWhileJsonConsumedInBatch()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Not yet implemented")]
    public Task Chunking_ShouldAbortSequence_WhenDisconnectingWhileBinaryMessageConsumedInBatch()
    {
        throw new NotImplementedException();
    }

    [Fact(Skip = "Not yet implemented")]
    public Task Chunking_ShouldAbortSequence_WhenDeserializationFailsWhileJsonConsumedInBatch()
    {
        // TODO: Test different error kinds (deserialization, processing, etc.) -> error mid batch, all sequences aborted and disposed?
        throw new NotImplementedException();
    }

    [Fact(Skip = "Not yet implemented")]
    public Task Chunking_ShouldAbortSequence_WhenProcessingFailsWhileJsonConsumedInBatch()
    {
        // TODO: Test different error kinds (deserialization, processing, etc.) -> error mid batch, all sequences aborted and disposed?
        throw new NotImplementedException();
    }

    [Fact(Skip = "Not yet implemented")]
    public Task Chunking_ShouldAbortSequence_WhenProcessingFailsWhileBinaryMessageConsumedInBatch()
    {
        // TODO: Test different error kinds (deserialization, processing, etc.) -> error mid batch, all sequences aborted and disposed?
        throw new NotImplementedException();
    }
}
