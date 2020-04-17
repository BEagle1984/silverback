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
    public class InMemoryChunkStoreTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _serviceProvider;
        private readonly BusConfigurator _configurator;
        private readonly OutboundInboundSubscriber _subscriber;

        public InMemoryChunkStoreTests()
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
                    .AddInMemoryChunkStore(retention: TimeSpan.FromMilliseconds(250))
                    .AddInboundConnector()
                    .AddDbOffsetStoredInboundConnector())
                .UseDbContext<TestDbContext>()
                .AddSingletonSubscriber<OutboundInboundSubscriber>();

            _serviceProvider = services.BuildServiceProvider();

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _subscriber = _serviceProvider.GetRequiredService<OutboundInboundSubscriber>();
        }

        [Fact(Skip = "Tested in BrokerBehaviorsPipelineTests")]
        public Task Chunking_ChunkedAndAggregatedCorrectly()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task SimpleChunking_AllChunksCommittedAtOnce()
        {
            var committedOffsets = new List<IOffset>();

            var message = new TestEventOne { Content = "Hello E2E!" };
            var rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(endpoints => endpoints
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawMessage.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();

            await producer.ProduceAsync(rawMessage.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();

            await producer.ProduceAsync(rawMessage.Skip(20).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Count.Should().Be(3);
        }

        [Fact]
        public async Task InterleavedMessages_IncompleteMessagesNotCommitted()
        {
            var committedOffsets = new List<IOffset>();

            var message1 = new TestEventOne { Content = "Hello E2E!" };
            var rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);
            var message2 = new TestEventOne { Content = "Hello E2E!" };
            var rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(endpoints => endpoints
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawMessage1.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(rawMessage1.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(rawMessage2.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(rawMessage1.Skip(20).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Count.Should().Be(1);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);

            await producer.ProduceAsync(rawMessage2.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Should().BeEmpty();
            _subscriber.InboundEnvelopes.Count.Should().Be(1);

            await producer.ProduceAsync(rawMessage2.Skip(20).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            committedOffsets.Count.Should().Be(6);
            _subscriber.InboundEnvelopes.Count.Should().Be(2);
            _subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }

        [Fact]
        // Note: This is expected to work just because of the OffsetStoredInboundConnector
        public async Task InterleavedMessages_SimulateConsumerCrashWith_MessagesProcessedOnce()
        {
            await _serviceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

            var message1 = new TestEventOne { Content = "Hello E2E!" };
            var rawMessage1 = await Endpoint.DefaultSerializer.SerializeAsync(
                message1,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);
            var message2 = new TestEventOne { Content = "Hello E2E!" };
            var rawMessage2 = await Endpoint.DefaultSerializer.SerializeAsync(
                message2,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var broker = _configurator.Connect(endpoints => endpoints
                .AddInbound<OffsetStoredInboundConnector>(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawMessage1.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage1.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage2.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage1.Skip(20).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            
            _subscriber.InboundEnvelopes.Count.Should().Be(1);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);

            broker.Disconnect();
            broker = _configurator.Connect(endpoints => endpoints
                .AddInbound<OffsetStoredInboundConnector>(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawMessage1.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage1.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage2.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage1.Skip(20).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });await producer.ProduceAsync(rawMessage2.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage2.Skip(20).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });

            _subscriber.InboundEnvelopes.Count.Should().Be(2);
            _subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            _subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
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

            var broker = _configurator.Connect(endpoints => endpoints
                .AddInbound(
                    new KafkaConsumerEndpoint("test-e2e"))).First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));

            await producer.ProduceAsync(rawMessage.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "123"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });

            var chunkStore = _serviceProvider.GetRequiredService<IChunkStore>();
            (await chunkStore.CountChunks("123")).Should().Be(2);

            await Task.Delay(250);

            await producer.ProduceAsync(rawMessage.Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });
            await producer.ProduceAsync(rawMessage.Skip(10).Take(10).ToArray(), new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, "456"),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, "3"),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)
            });

            await _serviceProvider.GetRequiredService<ChunkStoreCleaner>().Cleanup();

            (await chunkStore.CountChunks("123")).Should().Be(0);
            (await chunkStore.CountChunks("456")).Should().Be(2);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}