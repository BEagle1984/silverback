// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Chunking
{
    [Trait("Category", "E2E")]
    public class DbChunkStoreTests : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private readonly ServiceProvider _serviceProvider;

        private readonly IBusConfigurator _configurator;

        private readonly SpyBrokerBehavior _spyBehavior;

        public DbChunkStoreTests()
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
                        .AddDbChunkStore(TimeSpan.FromMilliseconds(250)))
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

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
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

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.OutboundEnvelopes.SelectMany(envelope => envelope.RawMessage).Should()
                .BeEquivalentTo(rawMessage);
            _spyBehavior.OutboundEnvelopes.ForEach(
                envelope =>
                {
                    envelope.RawMessage.Should().NotBeNull();
                    envelope.RawMessage!.Length.Should().BeLessOrEqualTo(10);
                });
            _spyBehavior.InboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
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

            var broker = _configurator.Connect(
                endpoints => endpoints
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"))).First();

            ((InMemoryConsumer)broker.Consumers[0]).CommitCalled +=
                (_, args) => committedOffsets.AddRange(args.Offsets);

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

            var broker = _configurator.Connect(
                endpoints => endpoints
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"))).First();

            ((InMemoryConsumer)broker.Consumers[0]).CommitCalled +=
                (_, args) => committedOffsets.AddRange(args.Offsets);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));

            using (var scope = _serviceProvider.CreateScope())
            {
                var chunkStore = scope.ServiceProvider.GetRequiredService<IChunkStore>();
                (await chunkStore.CountChunks("123")).Should().Be(2);
            }

            await Task.Delay(250);

            await producer.ProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 0, 3));
            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 1, 3));

            await _serviceProvider.GetRequiredService<ChunkStoreCleaner>().Cleanup();
            using (var scope = _serviceProvider.CreateScope())
            {
                var chunkStore = scope.ServiceProvider.GetRequiredService<IChunkStore>();
                (await chunkStore.CountChunks("123")).Should().Be(0);
                (await chunkStore.CountChunks("456")).Should().Be(2);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection == null)
                return;

            _connection.Close();
            await _connection.DisposeAsync();
        }
    }
}
