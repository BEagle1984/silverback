// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class SilverbackBuilderAddSubscriberExtensionsTests
    {
        [Fact]
        public void AddTransientSubscriber_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientSubscriber(typeof(TestSubscriber)));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddTransientSubscriberWithGenericArguments_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientSubscriber<TestSubscriber>());

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddTransientSubscriber_TypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientSubscriber(typeof(TestSubscriber), _ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddTransientSubscriberWithGenericArguments_TypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddTransientSubscriber(_ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddScopedSubscriber_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber(typeof(TestSubscriber)));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddScopedSubscriberWithGenericArguments_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber<TestSubscriber>());

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddScopedSubscriber_TypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber(typeof(TestSubscriber), _ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddScopedSubscriberWithGenericArguments_TypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber(_ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddSingletonSubscriber_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber(typeof(TestSubscriber)));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddSingletonSubscriberWithGenericArguments_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestSubscriber>());

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddSingletonSubscriber_TypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber(typeof(TestSubscriber), _ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddSingletonSubscriberWithGenericArguments_TypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber(_ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddSingletonSubscriber_TypeAndInstance_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber(typeof(TestSubscriber), new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddSingletonSubscriberWithGenericArguments_TypeAndInstance_SubscriberProperlyRegistered()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber(new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddScopedSubscriber_TypeWithAnnotatedMethodsOnly_MessagesReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestSubscriber>(autoSubscribeAllPublicMethods: false));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(2);
        }

        [Fact]
        public void AddSubscribers_Interface_MessagesReceived()
        {
            var testService1 = new TestServiceOne();
            var testService2 = new TestServiceTwo();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSubscribers<IService>()
                    .Services
                    .AddSingleton<IService>(testService1)
                    .AddSingleton(testService1)
                    .AddSingleton<IService>(testService2)
                    .AddSingleton(testService2));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new object[] { new TestCommandOne(), new TestCommandTwo() });

            testService1.ReceivedMessagesCount.Should().BeGreaterThan(0);
            testService2.ReceivedMessagesCount.Should().BeGreaterThan(0);
        }
    }
}
