// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class SilverbackBuilderExtensionsTests
    {
        [Fact]
        public void WithConnectionToMessageBroker_BrokerCollectionRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => { })
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBrokerCollection>().Should().NotBeNull();
        }

        [Fact]
        public void WithConnectionToMessageBroker_ActivityBehaviorsRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => { })
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(ActivityProducerBehavior));
            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(ActivityConsumerBehavior));
        }

#pragma warning disable 618

        [Fact]
        public void WithConnectionTo_BrokerCollectionRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBrokerCollection>().Should().NotBeNull();
            serviceProvider.GetService<IBrokerCollection>().Count.Should().Be(1);
        }

        [Fact]
        public void WithConnectionTo_BrokerRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            serviceProvider.GetServices<IBroker>().Count().Should().Be(1);
        }

        [Fact]
        public void WithConnectionTo_ActivityBehaviorsRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(ActivityProducerBehavior));
            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(ActivityConsumerBehavior));
        }

        [Fact]
        public void WithConnectionTo_BrokerOptionsConfiguratorInvoked()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(EmptyBehavior));
        }

#pragma warning restore 618
    }
}