// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherRoutingTests
    {
        [Fact]
        public async Task Publish_SubscribingExactType_MatchingMessagesReceived()
        {
            var receivedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEventOne message) => receivedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestCommandOne());

            receivedMessages.Should().HaveCount(2);
            receivedMessages.Should().AllBeOfType<TestEventOne>();
        }

        [Fact]
        public async Task Publish_SubscribingBaseType_MatchingMessagesReceived()
        {
            var receivedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((TestEvent message) => receivedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestCommandOne());

            receivedMessages.Should().HaveCount(4);
            receivedMessages.Should().AllBeAssignableTo<TestEvent>();
        }

        [Fact]
        public async Task Publish_SubscribingInterface_MatchingMessagesReceived()
        {
            var receivedMessages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((IEvent message) => receivedMessages.Add(message)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
            await publisher.PublishAsync(new TestCommandOne());

            receivedMessages.Should().HaveCount(4);
            receivedMessages.Should().AllBeAssignableTo<IEvent>();
        }
    }
}
