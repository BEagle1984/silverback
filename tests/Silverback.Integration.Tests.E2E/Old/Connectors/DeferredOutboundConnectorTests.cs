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
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Connectors
{
    [Trait("Category", "E2E")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Connection scoped to test method")]
    public class DeferredOutboundConnectorTests : E2ETestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        [Fact]
        public async Task OutboundMessages_QueuedAndPublishedByOutboundWorker()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddDbContext<TestDbContext>(
                            options => options
                                .UseSqlite(connection))
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddDbOutboundConnector()
                                .AddDbOutboundWorker(TimeSpan.FromMilliseconds(100)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .UseDbContext<TestDbContext>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await serviceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            Subscriber.OutboundEnvelopes.Count.Should().Be(3);
            Subscriber.InboundEnvelopes.Should().BeEmpty();

            await AsyncTestingUtil.WaitAsync(() => Subscriber.InboundEnvelopes.Count >= 3);

            Subscriber.InboundEnvelopes.Count.Should().Be(3);
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

            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddDbContext<TestDbContext>(
                            options => options
                                .UseSqlite(connection))
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddDbOutboundConnector()
                                .AddDbOutboundWorker(TimeSpan.FromMilliseconds(100)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .UseDbContext<TestDbContext>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await serviceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            Subscriber.InboundEnvelopes.Should().BeEmpty();

            await AsyncTestingUtil.WaitAsync(() => Subscriber.InboundEnvelopes.Count > 0);

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            var inboundEnvelope = Subscriber.InboundEnvelopes[0];
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
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddDbContext<TestDbContext>(
                            options => options
                                .UseSqlite(connection))
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddDbOutboundConnector()
                                .AddDbOutboundWorker(TimeSpan.FromMilliseconds(100)))
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
                        .UseDbContext<TestDbContext>()
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            Host.PauseBackgroundServices();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            await serviceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()).Should()
                .BeEquivalentTo(rawMessage);
            SpyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            SpyBehavior.InboundEnvelopes.Should().BeEmpty();

            Host.ResumeBackgroundServices();
            await AsyncTestingUtil.WaitAsync(() => SpyBehavior.InboundEnvelopes.Count > 0, 10000);

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

            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddDbContext<TestDbContext>(
                            options => options
                                .UseSqlite(connection))
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka()
                                .AddDbOutboundConnector()
                                .AddDbOutboundWorker(TimeSpan.FromMilliseconds(100)))
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
                        .UseDbContext<TestDbContext>()
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            Host.PauseBackgroundServices();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            await serviceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.OutboundEnvelopes[0].RawMessage.Should().NotBeEquivalentTo(rawMessage);
            SpyBehavior.InboundEnvelopes.Should().BeEmpty();

            Host.ResumeBackgroundServices();
            await AsyncTestingUtil.WaitAsync(() => SpyBehavior.InboundEnvelopes.Count > 0);

            SpyBehavior.InboundEnvelopes.Count.Should().Be(1);
            SpyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        }
    }
}
