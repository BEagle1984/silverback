// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public class PublisherBehaviorsTests
{
    [Fact]
    public async Task Publish_WithBehaviors_MessagesReceived()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonBehavior<TestBehavior>()
                .AddScopedBehavior<TestBehavior>()
                .AddTransientBehavior<TestBehavior>()
                .AddSingletonSubscriber<TestSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        TestSubscriber subscriber = serviceProvider.GetRequiredService<TestSubscriber>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        subscriber.ReceivedMessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task Publish_WithBehaviors_BehaviorsExecuted()
    {
        TestBehavior behavior1 = new();
        TestBehavior behavior2 = new();
        TestBehavior behavior3 = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonBehavior(behavior1)
                .AddScopedBehavior(_ => behavior2)
                .AddTransientBehavior(_ => behavior3));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        behavior1.EnterCount.Should().Be(2);
        behavior2.EnterCount.Should().Be(2);
        behavior3.EnterCount.Should().Be(2);
    }

    [Fact]
    public async Task Publish_WithSortedBehaviors_BehaviorsExecutedInExpectedOrder()
    {
        List<string> callsSequence = new();
        TestSortedBehavior behavior1 = new(100, callsSequence);
        TestSortedBehavior behavior2 = new(50, callsSequence);
        TestSortedBehavior behavior3 = new(-50, callsSequence);
        TestSortedBehavior behavior4 = new(-100, callsSequence);
        TestBehavior behavior5 = new(callsSequence);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonBehavior(behavior1)
                .AddScopedBehavior(_ => behavior2)
                .AddTransientBehavior(_ => behavior3)
                .AddSingletonBehavior(behavior4)
                .AddSingletonBehavior(behavior5));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        callsSequence.Should().BeEquivalentTo(
            new[]
            {
                "-100", "-50", "unsorted", "50", "100",
                "-100", "-50", "unsorted", "50", "100"
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Publish_MessageChangingBehavior_BehaviorExecuted()
    {
        List<object> receivedMessages = new();
        ChangeMessageBehavior<TestCommandOne> behavior = new(_ => new TestCommandTwo());
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonBehavior(behavior)
                .AddDelegateSubscriber((TestCommandTwo message) => receivedMessages.Add(message)));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        receivedMessages.Should().HaveCount(2);
        receivedMessages.Should().AllBeOfType<TestCommandTwo>();
    }
}
