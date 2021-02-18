// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherBehaviorsTests
    {
        [Fact]
        public async Task Publish_WithBehaviors_MessagesReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior<TestBehavior>()
                    .AddScopedBehavior<TestBehavior>()
                    .AddTransientBehavior<TestBehavior>()
                    .AddSingletonSubscriber<TestSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<TestSubscriber>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task Publish_WithBehaviors_BehaviorsExecuted()
        {
            var behavior1 = new TestBehavior();
            var behavior2 = new TestBehavior();
            var behavior3 = new TestBehavior();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior(behavior1)
                    .AddScopedBehavior(_ => behavior2)
                    .AddTransientBehavior(_ => behavior3));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            behavior1.EnterCount.Should().Be(2);
            behavior2.EnterCount.Should().Be(2);
            behavior3.EnterCount.Should().Be(2);
        }

        [Fact]
        public async Task Publish_WithSortedBehaviors_BehaviorsExecutedInExpectedOrder()
        {
            var callsSequence = new List<string>();
            var behavior1 = new TestSortedBehavior(100, callsSequence);
            var behavior2 = new TestSortedBehavior(50, callsSequence);
            var behavior3 = new TestSortedBehavior(-50, callsSequence);
            var behavior4 = new TestSortedBehavior(-100, callsSequence);
            var behavior5 = new TestBehavior(callsSequence);
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior(behavior1)
                    .AddScopedBehavior(_ => behavior2)
                    .AddTransientBehavior(_ => behavior3)
                    .AddSingletonBehavior(behavior4)
                    .AddSingletonBehavior(behavior5));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

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
            var receivedMessages = new List<object>();
            var behavior = new ChangeMessageBehavior<TestCommandOne>(_ => new TestCommandTwo());
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonBehavior(behavior)
                    .AddDelegateSubscriber((TestCommandTwo message) => receivedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            receivedMessages.Should().HaveCount(2);
            receivedMessages.Should().AllBeOfType<TestCommandTwo>();
        }
    }
}
