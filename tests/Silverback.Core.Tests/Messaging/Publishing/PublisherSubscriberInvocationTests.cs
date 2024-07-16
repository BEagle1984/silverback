// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherSubscriberInvocationTests
    {
        [Fact]
        public async Task Publish_ToSubscriberClass_MessagesReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<TestSubscriber>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(4);
            subscriber.ReceivedCallsCount.Should().Be(12);
        }

        [Fact]
        public async Task Publish_ToDelegateSubscriber_MessagesReceived()
        {
            int count = 0;
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((object _) => count++));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(4);
        }

        [Fact]
        public async Task Publish_ToDelegateSubscriberWithAdditionalParameters_MessagesReceived()
        {
            int count = 0;
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber<object>(
                        (_, forwardedServiceProvider) =>
                        {
                            if (forwardedServiceProvider != null)
                                count++;
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(4);
        }

        [Fact]
        public async Task Publish_ToMultipleSubscribers_MessagesReceived()
        {
            int delegateCount = 0;
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestSubscriber>()
                    .AddDelegateSubscriber(
                        (object _) =>
                        {
                            delegateCount++;
                            return Task.CompletedTask;
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<TestSubscriber>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(4);
            subscriber.ReceivedCallsCount.Should().Be(12);
            delegateCount.Should().Be(4);
        }

        [Fact]
        public async Task Publish_WithoutAutoSubscriptionOfPublicMethods_MessagesReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestSubscriber>(false));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<TestSubscriber>();

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TransactionCompletedEvent());
            publisher.Publish(new TransactionAbortedEvent());

            subscriber.ReceivedMessagesCount.Should().Be(3);
            subscriber.ReceivedCallsCount.Should().Be(6);
        }

        [Fact]
        public void Publish_SubscriberThrows_ExceptionReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestExceptionSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            Action act1 = () => publisher.Publish(new TestEventOne());
            Action act2 = () => publisher.Publish(new TestEventTwo());

            act1.Should().Throw<TargetInvocationException>();
            act2.Should().Throw<TargetInvocationException>();
        }

        [Fact]
        public async Task PublishAsync_SubscriberThrows_ExceptionReturned()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestExceptionSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            Func<Task> act1 = async () => await publisher.PublishAsync(new TestEventOne());
            Func<Task> act2 = async () => await publisher.PublishAsync(new TestEventTwo());

            await act1.Should().ThrowAsync<TargetInvocationException>();
            await act2.Should().ThrowAsync<TargetInvocationException>();
        }

        [Fact]
        public async Task Publish_ToExclusiveSubscribers_SequentiallyInvoked()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<ExclusiveSubscriberTestService>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<ExclusiveSubscriberTestService>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task Publish_ToNonExclusiveSubscribers_InvokedInParallel()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<NonExclusiveSubscriberTestService>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<NonExclusiveSubscriberTestService>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Parallel.Steps.Should().BeEquivalentTo(new[] { 1, 1, 3, 3 });
        }

        [Fact]
        public async Task Publish_ToExclusiveDelegateSubscribers_SequentiallyInvoked()
        {
            var parallel = new ParallelTestingUtil();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((ICommand _) => parallel.DoWork())
                    .AddDelegateSubscriber(
                        async (ICommand _) => await parallel.DoWorkAsync(),
                        new SubscriptionOptions { Exclusive = true }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            parallel.Steps.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task Publish_ToNonExclusiveDelegateSubscribers_InvokedInParallel()
        {
            var parallel = new ParallelTestingUtil();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (ICommand _) => parallel.DoWork(),
                        new SubscriptionOptions { Exclusive = false })
                    .AddDelegateSubscriber(
                        (ICommand _) => parallel.DoWorkAsync(),
                        new SubscriptionOptions { Exclusive = false }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());

            parallel.Steps.Should().BeEquivalentTo(new[] { 1, 1, 3, 3 });
        }

        [Fact]
        public async Task Publish_ToGenericSubscriber_MessageReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<EventOneGenericSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var subscriber = serviceProvider.GetRequiredService<EventOneGenericSubscriber>();

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_SubscriberNotRegisteredAsSelf_InvalidOperationExceptionIsThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddScoped<IService>(_ => new TestServiceOne())
                    .AddScoped<IService>(_ => new TestServiceTwo())
                    .AddSilverback()
                    .AddSubscribers<IService>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            Action act = () => publisher.Publish(new TestCommandOne());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task Publish_NotSubscribedMessageWithThrowIfUnhandled_ExceptionThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IEvent _) =>
                        {
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            Action actSync = () => publisher.Publish(new UnhandledMessage(), true);
            Func<Task> actAsync = () => publisher.PublishAsync(new UnhandledMessage(), true);

            actSync.Should().Throw<Exception>();
            await actAsync.Should().ThrowExactlyAsync<UnhandledMessageException>();
        }

        [Fact]
        public async Task Publish_SubscribedMessageWithThrowIfUnhandled_NoExceptionThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IEvent _) =>
                        {
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            Action actSync = () => publisher.Publish(new TestEventOne(), true);
            Func<Task> actAsync = () => publisher.PublishAsync(new TestEventOne(), true);

            actSync.Should().NotThrow();
            await actAsync.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Publish_NotSubscribedMessageWithoutThrowIfUnhandled_NoExceptionThrown()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IEvent _) =>
                        {
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            Action actSync = () => publisher.Publish(new UnhandledMessage());
            Func<Task> actAsync = () => publisher.PublishAsync(new UnhandledMessage());

            actSync.Should().NotThrow();
            await actAsync.Should().NotThrowAsync();
        }
    }
}
