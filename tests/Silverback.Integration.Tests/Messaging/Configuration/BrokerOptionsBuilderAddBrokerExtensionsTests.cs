// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class BrokerOptionsBuilderAddBrokerExtensionsTests
    {
        [Fact]
        public void AddBroker_ActivityBehaviorsRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should().Contain(behavior => behavior.GetType() == typeof(ActivityProducerBehavior));
            registeredBehaviors.Should().Contain(behavior => behavior.GetType() == typeof(ActivityConsumerBehavior));
        }

        [Fact]
        public void AddBroker_ExceptionLoggerBehaviorRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(behavior => behavior.GetType() == typeof(FatalExceptionLoggerConsumerBehavior));
        }

        [Fact]
        public void AddBroker_BrokerRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton(Substitute.For<IHostApplicationLifetime>())
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>())
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBroker>().Should().NotBeNull();
            serviceProvider.GetService<IBroker>().Should().BeOfType<TestBroker>();
        }

        [Fact]
        public void AddBroker_BrokerOptionsConfiguratorInvoked()
        {
            var serviceProvider = new ServiceCollection()
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>())
                .Services.BuildServiceProvider();

            var registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

            registeredBehaviors.Should()
                .Contain(x => x.GetType() == typeof(EmptyBehavior));
        }
    }
}
