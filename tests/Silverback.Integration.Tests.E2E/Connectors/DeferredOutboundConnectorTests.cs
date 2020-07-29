// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Connectors
{
    [Trait("Category", "E2E")]
    public class DeferredOutboundConnectorTests : IAsyncDisposable
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        private readonly SqliteConnection _connection;

        private readonly ServiceProvider _serviceProvider;

        private readonly IBusConfigurator _configurator;

        private readonly SpyBrokerBehavior _spyBehavior;

        private readonly OutboundInboundSubscriber _subscriber;

        public DeferredOutboundConnectorTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddDbContext<TestDbContext>(
                    options => options
                        .UseSqlite(_connection))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddInMemoryBroker()
                        .AddInMemoryChunkStore()
                        .AddDbOutboundConnector()
                        .AddDbOutboundWorker())
                .AddDbDistributedLockManager()
                .UseDbContext<TestDbContext>()
                .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _configurator = _serviceProvider.GetRequiredService<IBusConfigurator>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
        }

        [Fact]
        public async Task OutboundMessages_QueuedAndPublishedByOutboundWorker()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            _subscriber.OutboundEnvelopes.Count.Should().Be(3);
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await _serviceProvider.GetRequiredService<IOutboundQueueWorker>().ProcessQueue(CancellationToken.None);

            _subscriber.InboundEnvelopes.Count.Should().Be(3);
        }

        [Fact]
        public async Task MessageWithCustomHeaders_HeadersTransferred()
        {
            var message = new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "hi!",
                CustomHeader2 = true
            };

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await _serviceProvider.GetRequiredService<IOutboundQueueWorker>().ProcessQueue(CancellationToken.None);

            _subscriber.InboundEnvelopes.Count.Should().Be(1);
            var inboundEnvelope = _subscriber.InboundEnvelopes[0];
            inboundEnvelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header", "hi!"));
            inboundEnvelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header2", true));
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task Chunking_ChunkedAndAggregatedCorrectly()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            byte[]? rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(
                endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e")
                        {
                            Chunk = new ChunkSettings
                            {
                                Size = 10
                            }
                        })
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .BeEquivalentTo(rawMessage);
            _spyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            _spyBehavior.InboundEnvelopes.Should().BeEmpty();

            await _serviceProvider.GetRequiredService<IOutboundQueueWorker>().ProcessQueue(CancellationToken.None);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task Encryption_EncryptedAndDecrypted()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            byte[]? rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            _configurator.Connect(
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
                        }));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes[0].RawMessage.Should().NotBeEquivalentTo(rawMessage);
            _spyBehavior.InboundEnvelopes.Should().BeEmpty();

            await _serviceProvider.GetRequiredService<IOutboundQueueWorker>().ProcessQueue(CancellationToken.None);

            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection == null)
                return;

            _connection.Close();
            await _connection.DisposeAsync();

            await _serviceProvider.DisposeAsync();
        }
    }
}
