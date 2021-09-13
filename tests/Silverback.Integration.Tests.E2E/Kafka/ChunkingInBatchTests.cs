// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ChunkingInBatchTests : KafkaTestFixture
    {
        public ChunkingInBatchTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Chunking_JsonConsumedInBatch_ProducedAndConsumed()
        {
            var batches = new List<List<TestEventOne>>();
            string? failedCommit = null;
            string? enumerationAborted = null;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(3)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddDelegateSubscriber(
                            async (IAsyncEnumerable<TestEventOne> streamEnumerable) =>
                            {
                                var list = new List<TestEventOne>();
                                batches.ThreadSafeAdd(list);

                                await foreach (var message in streamEnumerable)
                                {
                                    list.Add(message);

                                    var actualCommittedOffsets =
                                        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                                    var expectedCommittedOffsets = 9 * (batches.Count - 1);

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
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 9; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"Long message {i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            failedCommit.Should().BeNull();
            enumerationAborted.Should().BeNull();

            batches.Should().HaveCount(3);
            batches[0].Should().HaveCount(3);
            batches[1].Should().HaveCount(3);
            batches[2].Should().HaveCount(3);

            batches[0][0].Content.Should().Be("Long message 1");
            batches[0][1].Content.Should().Be("Long message 2");
            batches[0][2].Content.Should().Be("Long message 3");

            batches[1][0].Content.Should().Be("Long message 4");
            batches[1][1].Content.Should().Be("Long message 5");
            batches[1][2].Content.Should().Be("Long message 6");

            batches[2][0].Content.Should().Be("Long message 7");
            batches[2][1].Content.Should().Be("Long message 8");
            batches[2][2].Content.Should().Be("Long message 9");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(27);
        }

        [Fact]
        public async Task Chunking_BinaryFileConsumedInBatch_ProducedAndConsumed()
        {
            var batches = new List<List<string?>>();
            string? failedCommit = null;
            string? enumerationAborted = null;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(5)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddDelegateSubscriber(
                            async (IAsyncEnumerable<BinaryFileMessage> streamEnumerable) =>
                            {
                                var list = new List<string?>();
                                batches.ThreadSafeAdd(list);

                                await foreach (var message in streamEnumerable)
                                {
                                    var actualCommittedOffsets =
                                        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                                    var expectedCommittedOffsets = 10 * (batches.Count - 1);

                                    if (actualCommittedOffsets != expectedCommittedOffsets)
                                    {
                                        failedCommit ??=
                                            $"{actualCommittedOffsets} != {expectedCommittedOffsets} " +
                                            $"({batches.Count}.{list.Count})";
                                    }

                                    var readAll = await message.Content.ReadAllAsync();
                                    list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
                                }

                                if (list.Count != 5)
                                {
                                    enumerationAborted ??=
                                        $"Enumeration completed after {list.Count} messages " +
                                        $"({batches.Count}.{list.Count})";
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new BinaryFileMessage(Encoding.UTF8.GetBytes($"Long message {i}")));
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            failedCommit.Should().BeNull();
            enumerationAborted.Should().BeNull();

            batches.Should().HaveCount(3);
            batches[0].Should().HaveCount(5);
            batches[1].Should().HaveCount(5);
            batches[2].Should().HaveCount(5);

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

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(30);
        }

        [Fact]
        public async Task Chunking_SingleChunkJsonConsumedInBatch_ProducedAndConsumed()
        {
            var batches = new List<List<TestEventOne>>();
            string? failedCommit = null;
            string? enumerationAborted = null;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(50))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(3)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddDelegateSubscriber(
                            async (IAsyncEnumerable<TestEventOne> streamEnumerable) =>
                            {
                                var list = new List<TestEventOne>();
                                batches.ThreadSafeAdd(list);

                                await foreach (var message in streamEnumerable)
                                {
                                    list.Add(message);

                                    var actualCommittedOffsets =
                                        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                                    var expectedCommittedOffsets = 3 * (batches.Count - 1);

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
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 9; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"Long message {i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            failedCommit.Should().BeNull();
            enumerationAborted.Should().BeNull();

            batches.Should().HaveCount(3);
            batches[0].Should().HaveCount(3);
            batches[1].Should().HaveCount(3);
            batches[2].Should().HaveCount(3);

            batches[0][0].Content.Should().Be("Long message 1");
            batches[0][1].Content.Should().Be("Long message 2");
            batches[0][2].Content.Should().Be("Long message 3");

            batches[1][0].Content.Should().Be("Long message 4");
            batches[1][1].Content.Should().Be("Long message 5");
            batches[1][2].Content.Should().Be("Long message 6");

            batches[2][0].Content.Should().Be("Long message 7");
            batches[2][1].Content.Should().Be("Long message 8");
            batches[2][2].Content.Should().Be("Long message 9");

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(9);
        }

        [Fact]
        public async Task Chunking_SingleChunkBinaryFileConsumedInBatch_ProducedAndConsumed()
        {
            var batches = new List<List<string?>>();
            string? failedCommit = null;
            string? enumerationAborted = null;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(50))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(5)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = DefaultConsumerGroupId;
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddDelegateSubscriber(
                            async (IAsyncEnumerable<BinaryFileMessage> streamEnumerable) =>
                            {
                                var list = new List<string?>();
                                batches.ThreadSafeAdd(list);

                                await foreach (var message in streamEnumerable)
                                {
                                    var actualCommittedOffsets =
                                        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName);
                                    var expectedCommittedOffsets = 5 * (batches.Count - 1);

                                    if (actualCommittedOffsets != expectedCommittedOffsets)
                                    {
                                        failedCommit ??=
                                            $"{actualCommittedOffsets} != {expectedCommittedOffsets} " +
                                            $"({batches.Count}.{list.Count})";
                                    }

                                    var readAll = await message.Content.ReadAllAsync();
                                    list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
                                }

                                if (list.Count != 5)
                                {
                                    enumerationAborted ??=
                                        $"Enumeration completed after {list.Count} messages " +
                                        $"({batches.Count}.{list.Count})";
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new BinaryFileMessage(Encoding.UTF8.GetBytes($"Long message {i}")));
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            failedCommit.Should().BeNull();
            enumerationAborted.Should().BeNull();

            batches.Should().HaveCount(3);
            batches[0].Should().HaveCount(5);
            batches[1].Should().HaveCount(5);
            batches[2].Should().HaveCount(5);

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

            DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_DisconnectWhileJsonConsumedInBatch_SequencesAborted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_DisconnectWhileBinaryFileConsumedInBatch_SequencesAborted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_DeserializationErrorWhileJsonConsumedInBatch_SequencesAborted()
        {
            // TODO: Test different error kinds (deserialization, processing, etc.) -> error mid batch, all sequences aborted and disposed?
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_ProcessingErrorWhileJsonConsumedInBatch_SequencesAborted()
        {
            // TODO: Test different error kinds (deserialization, processing, etc.) -> error mid batch, all sequences aborted and disposed?
            throw new NotImplementedException();
        }

        [Fact(Skip = "Not yet implemented")]
        public Task Chunking_ProcessingErrorWhileBinaryFileConsumedInBatch_SequencesAborted()
        {
            // TODO: Test different error kinds (deserialization, processing, etc.) -> error mid batch, all sequences aborted and disposed?
            throw new NotImplementedException();
        }
    }
}
