// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class EndpointsConfigurationTests
    {
        [Fact]
        public void AddOutbound_MultipleEndpoints_MessagesCorrectlyRouted()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddOutboundConnector())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<IIntegrationEvent>(new TestProducerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            broker.Connect();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            // -> to both endpoints
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            // -> to test2
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventTwo());

            // -> to nowhere
            publisher.Publish(new TestInternalEventOne());

            broker.ProducedMessages.Count.Should().Be(7);
            broker.ProducedMessages.Count(x => x.Endpoint.Name == "test1").Should().Be(2);
            broker.ProducedMessages.Count(x => x.Endpoint.Name == "test2").Should().Be(5);
        }

        [Fact]
        public void AddOutbound_WithMultipleBrokers_MessagesCorrectlyRouted()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddBroker<TestOtherBroker>()
                            .AddOutboundConnector())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                                .AddOutbound<TestEventTwo>(new TestOtherProducerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            broker.Connect();

            var otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
            otherBroker.Connect();

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventTwo());

            broker.ProducedMessages.Count.Should().Be(2);
            otherBroker.ProducedMessages.Count.Should().Be(3);
        }

        [Fact]
        public void AddInbound_MultipleEndpoints_ConsumersCorrectlyConnected()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddOutboundConnector())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddInbound(new TestConsumerEndpoint("test1"))
                                .AddInbound(new TestConsumerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            broker.Connect();

            broker.Consumers.Count.Should().Be(2);
        }

        [Fact]
        public void AddInbound_WithMultipleBrokers_ConsumersCorrectlyConnected()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddBroker<TestOtherBroker>()
                            .AddOutboundConnector())
                    .AddEndpoints(
                        endpoints =>
                            endpoints
                                .AddInbound(new TestConsumerEndpoint("test1"))
                                .AddInbound(new TestOtherConsumerEndpoint("test2"))));

            var broker = serviceProvider.GetRequiredService<TestBroker>();
            broker.Connect();

            var otherBroker = serviceProvider.GetRequiredService<TestOtherBroker>();
            otherBroker.Connect();

            broker.Consumers.Count.Should().Be(1);
            otherBroker.Consumers.Count.Should().Be(1);
        }
    }
}
