// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class BusConfiguratorTests
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private IServiceScope _serviceScope;

        private IServiceProvider GetServiceProvider() => _serviceProvider ??=
            _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        private IServiceProvider GetScopedServiceProvider() =>
            (_serviceScope ??= GetServiceProvider().CreateScope()).ServiceProvider;

        private TestBroker GetTestBroker() => GetServiceProvider().GetService<TestBroker>();
        private TestOtherBroker GetTestOtherBroker() => GetServiceProvider().GetService<TestOtherBroker>();
        private IPublisher GetPublisher() => GetScopedServiceProvider().GetService<IPublisher>();

        private BusConfigurator GetBusConfigurator() => GetServiceProvider().GetService<BusConfigurator>();

        public BusConfiguratorTests()
        {
            _services = new ServiceCollection();

            _services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            _serviceProvider = null; // Creation deferred to after AddBroker() has been called
            _serviceScope = null;

            InMemoryInboundLog.Clear();
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public void AddBroker_BrokerRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>())
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBroker>().Should().NotBeNull();
            serviceProvider.GetService<IBroker>().Should().BeOfType<TestBroker>();
        }

        [Fact]
        public void AddBroker_BrokerOptionsConfiguratorInvoked()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>())
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(EmptyBehavior));
        }

        [Fact]
        public void AddOutbound_MultipleEndpoints_MessagesCorrectlyRouted()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                .AddBroker<TestBroker>()
                .AddOutboundConnector());

            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                    .AddOutbound<IIntegrationEvent>(new TestProducerEndpoint("test2")));

            // -> to both endpoints
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventOne());
            // -> to test2
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            // -> to nowhere
            GetPublisher().Publish(new TestInternalEventOne());

            GetTestBroker().ProducedMessages.Count.Should().Be(7);
            GetTestBroker().ProducedMessages.Count(x => x.Endpoint.Name == "test1").Should().Be(2);
            GetTestBroker().ProducedMessages.Count(x => x.Endpoint.Name == "test2").Should().Be(5);
        }

        [Fact]
        public void AddOutbound_WithMultipleBrokers_MessagesCorrectlyRouted()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                .AddBroker<TestBroker>()
                .AddBroker<TestOtherBroker>()
                .AddOutboundConnector());

            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddOutbound<TestEventOne>(new TestProducerEndpoint("test1"))
                    .AddOutbound<TestEventTwo>(new TestOtherProducerEndpoint("test2")));

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventOne());

            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());

            GetTestBroker().ProducedMessages.Count.Should().Be(2);
            GetTestOtherBroker().ProducedMessages.Count.Should().Be(3);
        }

        [Fact]
        public void AddInbound_MultipleEndpoints_ConsumersCorrectlyConnected()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                .AddBroker<TestBroker>()
                .AddOutboundConnector());

            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddInbound(new TestConsumerEndpoint("test1"))
                    .AddInbound(new TestConsumerEndpoint("test2")));

            GetTestBroker().Consumers.Count.Should().Be(2);
        }

        [Fact]
        public void AddInbound_WithMultipleBrokers_ConsumersCorrectlyConnected()
        {
            _services.AddSilverback().WithConnectionToMessageBroker(options => options
                .AddBroker<TestBroker>()
                .AddBroker<TestOtherBroker>()
                .AddOutboundConnector());

            GetBusConfigurator().Connect(endpoints =>
                endpoints
                    .AddInbound(new TestConsumerEndpoint("test1"))
                    .AddInbound(new TestOtherConsumerEndpoint("test2")));

            GetTestBroker().Consumers.Count.Should().Be(1);
            GetTestOtherBroker().Consumers.Count.Should().Be(1);
        }

        [Fact]
        public void Connect_WithSomeEndpointConfigurators_EndpointsAreAdded()
        {
            _services
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .RegisterConfigurator<TestConfiguratorOne>());

            _services.AddEndpointsConfigurator<TestConfiguratorTwo>();

            GetBusConfigurator().Connect();

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventOne());

            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());

            GetTestBroker().ProducedMessages.Count.Should().Be(5);
        }

        [Fact]
        public void Connect_WithSMultipleBrokers_AllBrokersAreConnected()
        {
            _services
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddBroker<TestOtherBroker>());

            GetBusConfigurator().Connect();

            GetTestBroker().IsConnected.Should().BeTrue();
            GetTestOtherBroker().IsConnected.Should().BeTrue();
        }

        private class TestConfiguratorOne : IEndpointsConfigurator
        {
            public void Configure(IEndpointsConfigurationBuilder builder)
            {
                builder.AddOutbound<TestEventOne>(TestProducerEndpoint.GetDefault());
            }
        }

        private class TestConfiguratorTwo : IEndpointsConfigurator
        {
            public void Configure(IEndpointsConfigurationBuilder builder)
            {
                builder.AddOutbound<TestEventTwo>(TestProducerEndpoint.GetDefault());
            }
        }
    }
}