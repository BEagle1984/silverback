// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class BrokerOptionsBuilderAddBrokerExtensionsTests
{
    [Fact]
    public void AddBroker_ActivityBehaviorsRegisteredForDI()
    {
        ServiceProvider? serviceProvider = new ServiceCollection()
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
            .Services.BuildServiceProvider();

        List<IBrokerBehavior> registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

        registeredBehaviors.Should().Contain(behavior => behavior.GetType() == typeof(ActivityProducerBehavior));
        registeredBehaviors.Should().Contain(behavior => behavior.GetType() == typeof(ActivityConsumerBehavior));
    }

    [Fact]
    public void AddBroker_ExceptionLoggerBehaviorRegisteredForDI()
    {
        ServiceProvider? serviceProvider = new ServiceCollection()
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>())
            .Services.BuildServiceProvider();

        List<IBrokerBehavior> registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

        registeredBehaviors.Should().Contain(behavior => behavior.GetType() == typeof(FatalExceptionLoggerConsumerBehavior));
    }

    [Fact]
    public void AddBroker_BrokerRegisteredForDI()
    {
        ServiceProvider? serviceProvider = new ServiceCollection()
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger()
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
        ServiceProvider? serviceProvider = new ServiceCollection()
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(
                options => options
                    .AddBroker<TestBroker>())
            .Services.BuildServiceProvider();

        List<IBrokerBehavior> registeredBehaviors = serviceProvider.GetServices<IBrokerBehavior>().ToList();

        registeredBehaviors.Should()
            .Contain(x => x.GetType() == typeof(EmptyBehavior));
    }
}
