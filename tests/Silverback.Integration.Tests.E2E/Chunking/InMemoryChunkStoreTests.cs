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
using Silverback.Messaging.Connectors;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Chunking
{
    [Trait("Category", "E2E")]
    public class InMemoryChunkStoreTests : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private readonly ServiceProvider _serviceProvider;

        private readonly IBusConfigurator _configurator;

        private readonly OutboundInboundSubscriber _subscriber;

        public InMemoryChunkStoreTests()
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
                        .AddInMemoryChunkStore(TimeSpan.FromMilliseconds(250))
                        .AddInboundConnector()
                        .AddDbOffsetStoredInboundConnector())
                .UseDbContext<TestDbContext>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });

            _configurator = _serviceProvider.GetRequiredService<IBusConfigurator>();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();
        }

        [Fact]
        public void Chunking_ChunkedAndAggregatedCorrectly()
        {
            // This case is tested in BrokerBehaviorsPipelineTests
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task SimpleChunking_AllChunksCommittedAtOnce()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[]? rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
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
            committedOffsets.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));
            committedOffsets.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 3));
            committedOffsets.Count.Should().Be(3);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task InterleavedMessages_IncompleteMessagesNotCommitted()
        {
            var committedOffsets = new List<IOffset>();

            var message1 = new TestEventOne { Content = "Hello E2E!" };
            byte[]? rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var message2 = new TestEventOne { Content = "Hello E2E!" };
            byte[]? rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(
                endpoints => endpoints
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"))).First();

            ((InMemoryConsumer)broker.Consumers[0]).CommitCalled +=
                (_, args) => committedOffsets.AddRange(args.Offsets);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 0, 3));
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 3));
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Count.Should().Be(1);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);

            await producer.ProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 1, 3));
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Count.Should().Be(1);

            await producer.ProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 2, 3));
            committedOffsets.Count.Should().Be(6);
            _subscriber.InboundEnvelopes.Count.Should().Be(2);
            _subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task InterleavedMessages_SimulateConsumerCrashWith_MessagesProcessedOnce()
        {
            /* Note: This is expected to work just because of the OffsetStoredInboundConnector */

            using (var scope = _serviceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var message1 = new TestEventOne { Content = "Hello E2E!" };
            byte[]? rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var message2 = new TestEventOne { Content = "Hello E2E!" };
            byte[]? rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(
                endpoints => endpoints
                    .AddInbound<OffsetStoredInboundConnector>(new KafkaConsumerEndpoint("test-e2e"))).First();

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            await producer.ProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 0, 3));
            await producer.ProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 3));

            _subscriber.InboundEnvelopes.Count.Should().Be(1);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);

            broker.Disconnect();
            broker = _configurator.Connect(
                endpoints => endpoints
                    .AddInbound<OffsetStoredInboundConnector>(new KafkaConsumerEndpoint("test-e2e"))).First();

            producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            await producer.ProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 0, 3));
            await producer.ProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 3));
            await producer.ProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 1, 3));
            await producer.ProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 2, 3));

            _subscriber.InboundEnvelopes.Count.Should().Be(2);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            _subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task IncompleteMessages_CleanupAfterDefinedTimeout()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[]? rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
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

            await _serviceProvider.DisposeAsync();
        }
    }
}
