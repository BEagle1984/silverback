// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Chunking;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    [Trait("Category", "E2E")]
    public class ChunkingTests : E2ETestFixture
    {
        [Fact]
        public async Task Chunking_Json_ProducedAndConsumed()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 5).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = $"Long message {i}"
                        }));

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            Subscriber.OutboundEnvelopes.Count.Should().Be(5);
            Subscriber.InboundEnvelopes.Count.Should().Be(5);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(15);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            SpyBehavior.InboundEnvelopes.Count.Should().Be(5);

            var receivedContents =
                SpyBehavior.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).Content);

            receivedContents.Should()
                .BeEquivalentTo(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
        }

        [Fact]
        public async Task Chunking_BinaryFile_ProducedAndConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) => { receivedFiles.Add(binaryFile.Content.ReadAll()); })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            Subscriber.OutboundEnvelopes.Count.Should().Be(2);
            Subscriber.InboundEnvelopes.Count.Should().Be(2);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(2);
            receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
            receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());
        }

        [Fact]
        public async Task Chunking_BinaryFileReadAborted_NextMessageConsumed()
        {
            var message1 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf"
            };

            var message2 = new BinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
                        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x40,
                        0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x50
                    }),
                ContentType = "text/plain"
            };

            var receivedFiles = new List<byte[]?>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (BinaryFileMessage binaryFile) =>
                            {
                                if (binaryFile.ContentType != "text/plain")
                                {
                                    // Read only first chunk
                                    var buffer = new byte[10];
                                    binaryFile.Content!.Read(buffer, 0, 10);
                                    return;
                                }

                                receivedFiles.Add(binaryFile.Content.ReadAll());
                            })
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message1);
            await publisher.PublishAsync(message2);

            await DefaultTopic.WaitUntilAllMessagesAreConsumed();

            Subscriber.OutboundEnvelopes.Count.Should().Be(2);
            Subscriber.InboundEnvelopes.Count.Should().Be(2);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope => envelope.RawMessage.ReReadAll()!.Length.Should().Be(10));
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);

            SpyBehavior.InboundEnvelopes[0].Message.As<BinaryFileMessage>().ContentType.Should().Be("application/pdf");
            SpyBehavior.InboundEnvelopes[1].Message.As<BinaryFileMessage>().ContentType.Should().Be("text/plain");

            receivedFiles.Should().HaveCount(1);
            receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());
        }

        [Fact]
        public async Task Chunking_Json_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_Json_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_IncompleteJson_NextMessageConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_IncompleteBinaryFile_NextMessageConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_JsonMissingFirstChunk_NextMessageConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFileMissingFirstChunk_NextMessageConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_MultipleSequencesFromMultiplePartitions_ConcurrentlyConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_InterleavedChunks_ExceptionThrown()
        {
            throw new NotImplementedException();
        }

        // TODO: Test with concurrent consumers
    }
}
