// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Chunking;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E")]
    public class BrokerBehaviorsPipelineTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        [Fact]
        public async Task DefaultSettings_ProducedAndConsumed()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty)).ReadAll();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            Subscriber.OutboundEnvelopes.Count.Should().Be(1);
            Subscriber.InboundEnvelopes.Count.Should().Be(1);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task DefaultSettings_OffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e"))))
                .Run();

            var consumer = (InMemoryConsumer)serviceProvider.GetRequiredService<IBroker>().Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            committedOffsets.Count.Should().Be(1);
        }

        [Fact]
        public async Task MessageWithCustomHeaders_HeadersTransferred()
        {
            var message = new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header", "Hello header!"));
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header2", "False"));
        }

        [Fact]
        public async Task Chunking_ChunkedAndAggregatedCorrectly()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .BeEquivalentTo(rawMessage.ReadAll());
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task BatchConsuming_CorrectlyConsumedInBatch()
        {
            var message1 = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var message2 = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Batch = new BatchSettings
                                        {
                                            Size = 2
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(0);

            await publisher.PublishAsync(message2);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(2);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);
        }

        [Fact]
        public async Task ChunkingWithBatchConsuming_CorrectlyConsumedInBatchAndAggregated()
        {
            var message1 = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var message2 = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        Batch = new BatchSettings
                                        {
                                            Size = 2
                                        }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message1);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(0);

            await publisher.PublishAsync(message2);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(6);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            SpyBehavior.InboundEnvelopes.Count.Should().Be(2);
        }

        [Fact]
        public async Task ChunkingWithCustomHeaders_HeadersTransferred()
        {
            var message = new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header", "Hello header!"));
            SpyBehavior.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-custom-header2", "False"));
        }

        [Fact]
        public async Task ChunkingWithRemappedHeaderNames_HeadersRemappedAndMessageReceived()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
                                        Chunk = new ChunkSettings
                                        {
                                            Size = 10
                                        }
                                    })
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .WithCustomHeaderName(DefaultMessageHeaders.ChunkIndex, "x-ch-id")
                        .WithCustomHeaderName(DefaultMessageHeaders.ChunksCount, "x-ch-cnt")
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.Headers.GetValue("x-ch-id").Should().NotBeNullOrEmpty();
                    envelope.Headers.GetValue("x-ch-cnt").Should().NotBeNullOrEmpty();
                    envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex).Should().BeNull();
                    envelope.Headers.GetValue(DefaultMessageHeaders.ChunksCount).Should().BeNull();
                });

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task Encryption_EncryptedAndDecrypted()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint("test-e2e")
                                    {
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

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().NotBeEquivalentTo(rawMessage);
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task EncryptionAndChunking_EncryptedAndChunkedThenAggregatedAndDecrypted()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                                 message,
                                 new MessageHeaderCollection(),
                                 MessageSerializationContext.Empty) ??
                             throw new InvalidOperationException("Serializer returned null");

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
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

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(5);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().NotBeEquivalentTo(rawMessage.Read(10));
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }
    }
}
