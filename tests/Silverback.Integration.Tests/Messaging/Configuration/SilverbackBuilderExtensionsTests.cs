// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Diagnostics;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class SilverbackBuilderExtensionsTests
    {
        [Fact]
        public void WithConnectionTo_BrokerRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBroker>().Should().NotBeNull();
        }

        [Fact]
        public void WithConnectionTo_ActivityBehaviorsRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(ActivityProducerBehavior));
            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(ActivityConsumerBehavior));
        }
    }
}