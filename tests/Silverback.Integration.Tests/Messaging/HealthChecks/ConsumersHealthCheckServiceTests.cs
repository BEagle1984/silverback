// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.HealthChecks;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.HealthChecks
{
    public class ConsumersHealthCheckServiceTests
    {
        [Fact]
        public async Task CheckConsumersConnected_AllConnected_TrueReturned()
        {
            var connectedConsumer = Substitute.For<IConsumer>();
            connectedConsumer.IsConnected.Returns(true);

            var broker1 = Substitute.For<IBroker>();
            broker1.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker1.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker1.Consumers.Returns(
                new[]
                {
                    connectedConsumer, connectedConsumer, connectedConsumer
                });
            var broker2 = Substitute.For<IBroker>();
            broker2.ProducerEndpointType.Returns(typeof(TestOtherProducerEndpoint));
            broker2.ConsumerEndpointType.Returns(typeof(TestOtherConsumerEndpoint));
            broker2.Consumers.Returns(
                new[]
                {
                    connectedConsumer, connectedConsumer, connectedConsumer
                });

            var brokerCollection = new BrokerCollection(new[] { broker1, broker2 });

            var result = await new ConsumersHealthCheckService(brokerCollection).CheckConsumersConnected();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckConsumersConnected_OneIsNotConnected_FalseReturned()
        {
            var connectedConsumer = Substitute.For<IConsumer>();
            connectedConsumer.IsConnected.Returns(true);
            var disconnectedConsumer = Substitute.For<IConsumer>();
            connectedConsumer.IsConnected.Returns(false);

            var broker1 = Substitute.For<IBroker>();
            broker1.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker1.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker1.Consumers.Returns(
                new[]
                {
                    connectedConsumer, connectedConsumer, connectedConsumer
                });
            var broker2 = Substitute.For<IBroker>();
            broker2.ProducerEndpointType.Returns(typeof(TestOtherProducerEndpoint));
            broker2.ConsumerEndpointType.Returns(typeof(TestOtherConsumerEndpoint));
            broker2.Consumers.Returns(
                new[]
                {
                    connectedConsumer, disconnectedConsumer, connectedConsumer
                });

            var brokerCollection = new BrokerCollection(new[] { broker1, broker2 });

            var result = await new ConsumersHealthCheckService(brokerCollection).CheckConsumersConnected();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckConsumersConnected_NoConsumers_TrueReturned()
        {
            var broker1 = Substitute.For<IBroker>();
            broker1.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker1.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            broker1.Consumers.Returns(Array.Empty<IConsumer>());

            var brokerCollection = new BrokerCollection(new[] { broker1 });

            var result = await new ConsumersHealthCheckService(brokerCollection).CheckConsumersConnected();

            result.Should().BeTrue();
        }
    }
}
