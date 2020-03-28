// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E
{
    public class BrokerBehaviorsPipelineTests
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        private readonly ServiceProvider _serviceProvider;
        private readonly BusConfigurator _configurator;
        private readonly SpyBrokerBehavior _spyBehavior;
        private readonly OutboundInboundSubscriber _subscriber;

        public BrokerBehaviorsPipelineTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker()
                    .AddChunkStore<InMemoryChunkStore>())
                .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider();

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
        }

        [Fact]
        public async Task E2E_DefaultSettings()
        {
            var message = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _subscriber.OutboundEnvelopes.Count.Should().Be(1);
            _subscriber.InboundEnvelopes.Count.Should().Be(1);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes.First().RawMessage.Should().BeEquivalentTo(rawMessage);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.First().Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task E2E_Chunking()
        {
            var message = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 10
                        }
                    })
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(7);
            _spyBehavior.OutboundEnvelopes.ForEach(envelope => envelope.RawMessage.Length.Should().BeLessOrEqualTo(10));
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.First().Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task E2E_BatchConsuming()
        {
            var message1 = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };
            var message2 = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"),
                    settings: new InboundConnectorSettings
                    {
                        Batch = new BatchSettings
                        {
                            Size = 2
                        }
                    }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message1);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(0);

            await publisher.PublishAsync(message2);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(2);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(2);
        }

        [Fact]
        public async Task E2E_ChunkingWithBatchConsuming()
        {
            var message1 = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };
            var message2 = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };

            _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 10
                        }
                    })
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"),
                    settings: new InboundConnectorSettings
                    {
                        Batch = new BatchSettings
                        {
                            Size = 2
                        }
                    }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message1);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(7);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(0);

            await publisher.PublishAsync(message2);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(14);
            _spyBehavior.OutboundEnvelopes.ForEach(envelope => envelope.RawMessage.Length.Should().BeLessOrEqualTo(10));
            _spyBehavior.InboundEnvelopes.Count.Should().Be(2);
        }

        [Fact]
        public async Task E2E_Encryption()
        {
            var message = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(endpoints => endpoints
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
                    }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes.First().RawMessage.Should().NotBeEquivalentTo(rawMessage);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.First().Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task E2E_EncryptionAndChunking()
        {
            var message = new TestEventOne
            {
                Id = Guid.NewGuid(),
                Content = "Hello E2E!"
            };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(endpoints => endpoints
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
                    }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _subscriber.OutboundEnvelopes.Count.Should().Be(1);
            _subscriber.OutboundEnvelopes.First().RawMessage.Should().NotBeEquivalentTo(rawMessage);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(10);
            _spyBehavior.OutboundEnvelopes.ForEach(envelope => envelope.RawMessage.Length.Should().BeLessOrEqualTo(10));
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.First().Message.Should().BeEquivalentTo(message);
        }
    }
}