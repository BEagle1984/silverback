// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class BrokerCollectionTests
    {
        private readonly IProducerEndpoint[] _producerEndpoints =
            { TestProducerEndpoint.GetDefault(), TestOtherProducerEndpoint.GetDefault() };

        private readonly IConsumerEndpoint[] _consumerEndpoints =
            { TestConsumerEndpoint.GetDefault(), TestOtherConsumerEndpoint.GetDefault() };

        [Theory]
        [InlineData(0, "TestProducer")]
        [InlineData(1, "TestOtherProducer")]
        public void GetProducer_WithMultipleBrokers_RightProducerInstanceIsReturned(
            int endpointIndex,
            string expectedProducerType)
        {
            var brokerCollection = new BrokerCollection(new IBroker[]
            {
                new TestBroker(Substitute.For<IServiceProvider>(), null),
                new TestOtherBroker(Substitute.For<IServiceProvider>(), null)
            });
            var endpoint = _producerEndpoints[endpointIndex];

            var producer = brokerCollection.GetProducer(endpoint);

            producer.Should().NotBeNull();
            producer.GetType().Name.Should().BeEquivalentTo(expectedProducerType);
        }

        [Theory]
        [InlineData(0, "TestConsumer")]
        [InlineData(1, "TestOtherConsumer")]
        public void GetConsumer_WithMultipleBrokers_RightConsumerInstanceIsReturned(
            int endpointIndex,
            string expectedConsumerType)
        {
            var brokerCollection = new BrokerCollection(new IBroker[]
            {
                new TestBroker(Substitute.For<IServiceProvider>(), null),
                new TestOtherBroker(Substitute.For<IServiceProvider>(), null)
            });
            var endpoint = _consumerEndpoints[endpointIndex];

            var consumer = brokerCollection.GetConsumer(endpoint);

            consumer.Should().NotBeNull();
            consumer.GetType().Name.Should().BeEquivalentTo(expectedConsumerType);
        }
    }
}