// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Chunking
{
    [Trait("Category", "E2E"), Collection("StaticInMemory")]
    public class InMemoryChunkStoreTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly BusConfigurator _configurator;
        private readonly SpyBrokerBehavior _spyBehavior;
        private readonly OutboundInboundSubscriber _subscriber;

        public InMemoryChunkStoreTests()
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

            InMemoryChunkStore.Clear();
        }

        [Fact(Skip = "Tested in BrokerBehaviorsPipelineTests")]
        public Task Chunking_ChunkedAndAggregatedCorrectly()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Functionality not yet implemented")]
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

        [Fact(Skip = "Functionality not yet implemented")]
        public async Task InterleavedMessages_IncompleteMessagesNotCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact(Skip = "Functionality not yet implemented")]
        public async Task IncompleteMessages_CleanupAfterDefinedTimeout()
        {
            throw new NotImplementedException();
        }
    }
}