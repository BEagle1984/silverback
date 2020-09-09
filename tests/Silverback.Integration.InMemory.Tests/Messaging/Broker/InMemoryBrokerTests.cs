// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.InMemory.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.InMemory.Messaging.Broker
{
    public class InMemoryBrokerTests
    {
        [Fact]
        public void GetProducer_ReturnsNewInMemoryProducer()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback().WithConnectionToMessageBroker(options => options.AddInMemoryBroker()));

            var endpoint = new KafkaProducerEndpoint("test");
            var producer = serviceProvider.GetRequiredService<IBroker>().GetProducer(endpoint);

            producer.Should().NotBeNull();
            producer.Should().BeOfType<InMemoryProducer>();
        }

        [Fact]
        public void AddConsumer_ReturnsNewInMemoryConsumer()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback().WithConnectionToMessageBroker(options => options.AddInMemoryBroker()));

            var endpoint = new KafkaConsumerEndpoint("test");
            var consumer = serviceProvider.GetRequiredService<IBroker>().AddConsumer(endpoint);

            consumer.Should().NotBeNull();
            consumer.Should().BeOfType<InMemoryConsumer>();
        }

        [Fact]
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public void InMemoryBroker_ProduceMessage_MessageConsumed()
        {
            var endpointName = "test";
            var receivedMessages = new List<object>();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddInMemoryBroker())
                    .AddDelegateSubscriber((IRawInboundEnvelope envelope) =>
            {
                var (deserialized, _) = envelope.Endpoint.Serializer.Deserialize(
                    envelope.RawMessage,
                    new MessageHeaderCollection(envelope.Headers),
                    MessageSerializationContext.Empty);

                if (deserialized != null)
                    receivedMessages.Add(deserialized);
            }));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            broker.AddConsumer(new KafkaConsumerEndpoint(endpointName));

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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddInMemoryBroker())
                    .AddDelegateSubscriber(
                        (IRawInboundEnvelope envelope) =>
                        {
                            var (deserialized, _) = envelope.Endpoint.Serializer.Deserialize(
                                envelope.RawMessage,
                                new MessageHeaderCollection(envelope.Headers),
                                MessageSerializationContext.Empty);

                            if (deserialized != null)
                                receivedMessages.Add(deserialized);
                        }));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            broker.AddConsumer(new KafkaConsumerEndpoint(endpointName));

            producer.Produce(new TestMessage { Content = "hello!" });

            receivedMessages.First().Should().BeOfType<TestMessage>();
            receivedMessages.OfType<TestMessage>().First().Content.Should().Be("hello!");
        }

        [Fact]
        public void InMemoryBroker_ProduceMessage_MessageHeadersReceived()
        {
            var endpointName = "test";
            var receivedHeaders = new List<IEnumerable<MessageHeader>>();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddInMemoryBroker())
                    .AddDelegateSubscriber(
                        (IRawInboundEnvelope envelope) =>
                        {
                            receivedHeaders.Add(envelope.Headers);
                        }));

            var broker = serviceProvider.GetRequiredService<IBroker>();
            var producer = broker.GetProducer(new KafkaProducerEndpoint(endpointName));
            broker.AddConsumer(new KafkaConsumerEndpoint(endpointName));

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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback().WithConnectionToMessageBroker(
                        options => options
                            .AddInMemoryBroker())
                    .AddDelegateSubscriber((IInboundEnvelope<TestMessage> envelope) => receivedEnvelopes.Add(envelope))
                    .AddEndpoints(
                        endpoints => endpoints
                            .AddInbound(new KafkaConsumerEndpoint(endpointName))
                            .AddOutbound<TestMessage>(new KafkaProducerEndpoint(endpointName))));

            serviceProvider.GetRequiredService<IBroker>().Connect();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestMessage { Content = "hello!" });
            publisher.Publish(new TestMessage { Content = "hello 2!" });

            receivedEnvelopes.Count.Should().Be(2);
            receivedEnvelopes.OfType<IInboundEnvelope>().Select(x => x.Message).Should().AllBeOfType<TestMessage>();
        }

        [Fact]
        public void InMemoryBroker_ConnectAndDispose_NoExceptionIsThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback().WithConnectionToMessageBroker(
                        options => options
                            .AddInMemoryBroker())
                    .AddEndpoints(
                        endpoints => endpoints
                            .AddInbound(new KafkaConsumerEndpoint("test"))
                            .AddOutbound<TestMessage>(new KafkaProducerEndpoint("test"))));

            var broker = serviceProvider.GetRequiredService<InMemoryBroker>();

            broker.Connect();
            broker.Dispose();
        }
    }
}
