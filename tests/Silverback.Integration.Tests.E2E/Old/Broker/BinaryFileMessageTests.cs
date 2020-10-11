// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Old.Broker
{
    public sealed class BinaryFileMessageTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        [Fact(Skip = "Deprecated")]
        public async Task DefaultSettings_ProducedAndConsumed()
        {
            var message = new BinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }),
                ContentType = "application/pdf"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(message.Content.ReReadAll());
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Should().BeAssignableTo<IInboundEnvelope<BinaryFileMessage>>();
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("content-type", "application/pdf"));
            var inboundBinaryFile = (IInboundEnvelope<BinaryFileMessage>)SpyBehavior.InboundEnvelopes[0];
            inboundBinaryFile.Message.Should().NotBeNull();
            inboundBinaryFile.Message!.ContentType.Should().BeEquivalentTo(message.ContentType);
            inboundBinaryFile.Message!.Content.ReReadAll().Should().BeEquivalentTo(message.Content.ReReadAll());
        }

        [Fact(Skip = "Deprecated")]
        public async Task InheritedBinaryFileMessage_ProducedAndConsumed()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }),
                ContentType = "application/pdf",
                CustomHeader = "hello!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header", "hello!"));
        }

        [Fact(Skip = "Deprecated")]
        public async Task ForcedDefaultBinaryFileDeserializer_ProducedAndConsumed()
        {
            var message = new BinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }),
                ContentType = "application/pdf"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
        }

        [Fact(Skip = "Deprecated")]
        public async Task ForcedDefaultBinaryFileSerializerAndDeserializer_ProducedAndConsumed()
        {
            var message = new BinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }),
                ContentType = "application/pdf"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Serializer = BinaryFileMessageSerializer.Default
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().BeEquivalentTo(message.Content);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("content-type", "application/pdf"));
        }

        [Fact(Skip = "Deprecated")]
        public async Task BinaryFileMessageWithoutHeaders_ProducedAndConsumed()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawContent);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .BeEquivalentTo(rawContent);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeAssignableTo<IBinaryFileMessage>();
            SpyBehavior.InboundEnvelopes[0].Message.As<IBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(rawContent);
        }

        [Fact(Skip = "Deprecated")]
        public async Task BinaryFileMessageWithoutHeadersAndForcedDeserializer_ProducedAndConsumed()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Serializer = BinaryFileMessageSerializer.Default
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawContent);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .BeEquivalentTo(rawContent);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeAssignableTo<IBinaryFileMessage>();
            SpyBehavior.InboundEnvelopes[0].Message.As<IBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(rawContent);
        }

        [Fact(Skip = "Deprecated")]
        public async Task InheritedBinaryWithoutTypeHeader_ProducedAndConsumed()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new[]
            {
                new MessageHeader("x-custom-header", "hello!")
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawContent, headers);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .BeEquivalentTo(rawContent);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<InheritedBinaryFileMessage>();
            SpyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(rawContent);
            SpyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().CustomHeader.Should()
                .Be("hello!");
        }

        [Fact(Skip = "Deprecated")]
        public async Task EncryptionAndChunking_EncryptedAndChunkedThenAggregatedAndDecrypted()
        {
            var message = new BinaryFileMessage
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

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        },
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(5);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .NotBeEquivalentTo(message.Content.ReadAll());
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(30);
                });
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact(Skip = "Deprecated")]
        public async Task EncryptionAndChunkingOfInheritedBinary_EncryptedAndChunkedThenAggregatedAndDecrypted()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new MemoryStream(
                    new byte[]
                    {
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10,
                        0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x20,
                        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30
                    }),
                ContentType = "application/pdf",
                CustomHeader = "hello!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IBinaryFileMessage>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        },
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Encryption = new SymmetricEncryptionSettings
                                        {
                                            Key = AesEncryptionKey
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(5);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .NotBeEquivalentTo(message.Content.ReadAll());
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(30);
                });
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeOfType<InheritedBinaryFileMessage>();
            SpyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().Content.Should()
                .BeEquivalentTo(message.Content);
            SpyBehavior.InboundEnvelopes[0].Message.As<InheritedBinaryFileMessage>().CustomHeader.Should()
                .Be("hello!");
        }
    }
}
