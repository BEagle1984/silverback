// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Connectors
{
    [Trait("Category", "E2E")]
    public class OffsetStoredInboundConnectorTests : IDisposable
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };
        
        private readonly SqliteConnection _connection;

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private readonly ServiceProvider _serviceProvider;

        private readonly BusConfigurator _configurator;
        private readonly OutboundInboundSubscriber _subscriber;

        public OffsetStoredInboundConnectorTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddDbContext<TestDbContext>(options => options
                    .UseSqlite(_connection))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker()
                    .AddInMemoryChunkStore()
                    .AddDbOffsetStoredInboundConnector())
                .UseDbContext<TestDbContext>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
        }

        [Fact]
        public async Task DuplicatedMessages_ConsumedOnce()
        {
            var message1 = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers1 = new MessageHeaderCollection();
            var rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                headers1,
                MessageSerializationContext.Empty);

            var message2 = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var headers2 = new MessageHeaderCollection();
            var rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                headers2,
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(endpoints => endpoints
                .AddOutbound<IIntegrationEvent>(
                    new KafkaProducerEndpoint("test-e2e"))
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            var consumer = (InMemoryConsumer) broker.Consumers.First();
            await consumer.Receive(rawMessage1, headers1, new InMemoryOffset("test-e2e", 1));
            await consumer.Receive(rawMessage2, headers2, new InMemoryOffset("test-e2e", 2));
            await consumer.Receive(rawMessage1, headers1, new InMemoryOffset("test-e2e", 1));
            await consumer.Receive(rawMessage2, headers2, new InMemoryOffset("test-e2e", 2));

            _subscriber.InboundEnvelopes.Count.Should().Be(2);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            _subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }
        
        [Fact]
        public async Task RetryPolicy_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"),
                        policy => policy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _subscriber.InboundEnvelopes.Count.Should().Be(3);
        }

                [Fact]
        public async Task EncryptionAndChunkingWithRetries_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            byte[] rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var tryCount = 0;

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e")
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
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")
                        {
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        },
                        policy => policy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            _subscriber.InboundEnvelopes.Count.Should().Be(3);

        }
        
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}