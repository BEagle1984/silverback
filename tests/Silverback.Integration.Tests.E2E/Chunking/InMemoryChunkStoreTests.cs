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
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Chunking
{
    [Trait("Category", "E2E")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Connection scoped to test method")]
    public class InMemoryChunkStoreTests : E2ETestFixture
    {
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
            byte[] rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty) ??
                                throw new InvalidOperationException("Serializer returned null");

            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore()
                                .AddInboundConnector())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e"))))
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = (InMemoryConsumer)broker.Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

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

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore()
                                .AddInboundConnector())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var consumer = (InMemoryConsumer)broker.Consumers[0];
            consumer.CommitCalled += (_, args) => committedOffsets.AddRange(args.Offsets);

            var producer = broker.GetProducer(new KafkaProducerEndpoint("test-e2e"));
            await producer.ProduceAsync(
                rawMessage1.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 0, 3));
            committedOffsets.Should().BeEmpty();
            Subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage1.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 1, 3));
            committedOffsets.Should().BeEmpty();
            Subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage2.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 0, 3));
            committedOffsets.Should().BeEmpty();
            Subscriber.InboundEnvelopes.Should().BeEmpty();

            await producer.ProduceAsync(
                rawMessage1.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("123", 2, 3));
            committedOffsets.Should().BeEmpty();
            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            Subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);

            await producer.ProduceAsync(
                rawMessage2.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 1, 3));
            committedOffsets.Should().BeEmpty();
            Subscriber.InboundEnvelopes.Count.Should().Be(1);

            await producer.ProduceAsync(
                rawMessage2.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders<TestEventOne>("456", 2, 3));
            committedOffsets.Count.Should().Be(6);
            Subscriber.InboundEnvelopes.Count.Should().Be(2);
            Subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
        }

        [Fact]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task InterleavedMessages_SimulateConsumerCrashWith_MessagesProcessedOnce()
        {
            /* Note: This is expected to work just because of the OffsetStoredInboundConnector */

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
                                .AddInMemoryChunkStore()
                                .AddDbOffsetStoredInboundConnector())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .UseDbContext<TestDbContext>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            using (var scope = Host.ServiceProvider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();
            }

            var broker = serviceProvider.GetRequiredService<InMemoryBroker>();
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

            Subscriber.InboundEnvelopes.Count.Should().Be(1);
            Subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);

            broker.Disconnect();
            broker.ResetOffsets();
            broker.Connect();

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

            Subscriber.InboundEnvelopes.Count.Should().Be(2);
            Subscriber.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message1);
            Subscriber.InboundEnvelopes[1].Message.Should().BeEquivalentTo(message2);
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

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddInMemoryBroker()
                                .AddInMemoryChunkStore(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(50))
                                .AddInboundConnector())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

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
