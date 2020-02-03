// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.InMemory.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.InMemory.Messaging.Broker
{
    public class InMemoryBrokerTests
    {
        private readonly IServiceProvider _serviceProvider;

        public InMemoryBrokerTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            services.AddSilverback().WithInMemoryBroker();

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        [Fact]
        public void GetProducer_ReturnsNewInMemoryProducer()
        {
            var endpoint = new KafkaProducerEndpoint("test");

            var producer = _serviceProvider.GetRequiredService<IBroker>().GetProducer(endpoint);

            producer.Should().NotBeNull();
            producer.Should().BeOfType<InMemoryProducer>();
        }

        [Fact]
        public void GetConsumer_ReturnsNewInMemoryConsumer()
        {
            var endpoint = new KafkaConsumerEndpoint("test");

            var consumer = _serviceProvider.GetRequiredService<IBroker>().GetConsumer(endpoint);

            consumer.Should().NotBeNull();
            consumer.Should().BeOfType<InMemoryConsumer>();
        }

        [Fact]
        public void InMemoryBroker_ProduceMessage_MessageConsumed()
        {
            var endpointName = "test";
            var receivedMessages = new List<object>();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            var consumer = broker.GetConsumer(new KafkaConsumerEndpoint(endpointName));
#pragma warning disable 1998
            consumer.Received += async (_, e) =>
                receivedMessages.Add(e.Envelope.Endpoint.Serializer.Deserialize(e.Envelope.RawMessage,
                    new MessageHeaderCollection(e.Envelope.Headers)));
#pragma warning restore 1998

            producer.Produce(new TestMessage { Content = "hello!" });
            producer.Produce(new TestMessage { Content = "hello 2!" });

            receivedMessages.Count.Should().Be(2);
            receivedMessages.Should().AllBeOfType<TestMessage>();
        }

        [Fact]
        public void InMemoryBroker_ProduceMessage_MessageReceived()
        {
            var endpointName = "test";
            var receivedMessages = new List<object>();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            var consumer = broker.GetConsumer(new KafkaConsumerEndpoint(endpointName));
#pragma warning disable 1998
            consumer.Received += async (_, e) =>
                receivedMessages.Add(e.Envelope.Endpoint.Serializer.Deserialize(e.Envelope.RawMessage,
                    new MessageHeaderCollection(e.Envelope.Headers)));
#pragma warning restore 1998

            producer.Produce(new TestMessage { Content = "hello!" });

            receivedMessages.First().Should().BeOfType<TestMessage>();
            receivedMessages.OfType<TestMessage>().First().Content.Should().Be("hello!");
        }

        [Fact]
        public void InMemoryBroker_ProduceMessage_MessageHeadersReceived()
        {
            var endpointName = "test";
            var receivedHeaders = new List<IEnumerable<MessageHeader>>();

            var broker = _serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            var consumer = broker.GetConsumer(new KafkaConsumerEndpoint(endpointName));
#pragma warning disable 1998
            consumer.Received += async (_, e) => receivedHeaders.Add(e.Envelope.Headers);
#pragma warning restore 1998

            producer.Produce(
                new TestMessage { Content = "hello!" },
                new[] { new MessageHeader("a", "b"), new MessageHeader("c", "d") });

            receivedHeaders.First().Should().ContainEquivalentOf(new MessageHeader("a", "b"));
            receivedHeaders.First().Should().ContainEquivalentOf(new MessageHeader("c", "d"));
        }

        [Fact]
        public void InMemoryBroker_PublishMessageThroughConnector_MessageConsumed()
        {
            var endpointName = "test";
            var receivedEnvelopes = new List<object>();

            _serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe((IInboundEnvelope<TestMessage> envelope) => receivedEnvelopes.Add(envelope))
                .Connect(endpoints => endpoints
                    .AddInbound(new KafkaConsumerEndpoint(endpointName))
                    .AddOutbound<TestMessage>(new KafkaProducerEndpoint(endpointName)));

            using (var scope = _serviceProvider.CreateScope())
            {
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                publisher.Publish(new TestMessage { Content = "hello!" });
                publisher.Publish(new TestMessage { Content = "hello 2!" });
            }

            receivedEnvelopes.Count.Should().Be(2);
            receivedEnvelopes.OfType<IInboundEnvelope>().Select(x => x.Message).Should().AllBeOfType<TestMessage>();
        }
    }
}