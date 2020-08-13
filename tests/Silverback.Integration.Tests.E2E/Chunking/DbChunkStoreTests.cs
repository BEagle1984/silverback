// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Chunking
{
    [Trait("Category", "E2E")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Connection scoped to test method")]
    public class DbChunkStoreTests : E2ETestFixture
    {
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
                                .AddInMemoryBroker()
                                .AddDbChunkStore())
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

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            SpyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            SpyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .BeEquivalentTo(rawMessage);
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
        public async Task SimpleChunking_EachChunkImmediatelyCommitted()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddDbChunkStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .UseDbContext<TestDbContext>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = (InMemoryConsumer)broker.Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));
            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            committedOffsets.Count.Should().Be(1);

            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));
            committedOffsets.Count.Should().Be(2);

            await producer.ProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 3));
            committedOffsets.Count.Should().Be(3);
        }

        [Fact]
        public async Task IncompleteMessages_CleanupAfterDefinedTimeout()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
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
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddDbChunkStore(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(50)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .UseDbContext<TestDbContext>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = (InMemoryConsumer)broker.Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));
            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));

            await Task.Delay(100);

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                var chunkStore = scope.ServiceProvider.GetRequiredService<IChunkStore>();
                (await chunkStore.CountChunks("123")).Should().Be(2);

                await AsyncTestingUtil.WaitAsync(async () => await chunkStore.CountChunks("123") == 0, 500);
            }

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 1, 3));

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                var chunkStore = scope.ServiceProvider.GetRequiredService<IChunkStore>();
                (await chunkStore.CountChunks("123")).Should().Be(0);
                (await chunkStore.CountChunks("456")).Should().Be(2);
            }
        }
    }
}
