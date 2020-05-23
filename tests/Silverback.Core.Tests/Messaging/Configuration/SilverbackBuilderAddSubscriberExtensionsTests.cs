// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class SilverbackBuilderAddSubscriberExtensionsTests
    {
        [Fact]
        public void AddTransientSubscriber_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddTransientSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber)));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddTransientSubscriber_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddTransientSubscriber(typeof(TestSubscriber)));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddTransientSubscriberWithGenericArguments_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddTransientSubscriber<ITestSubscriber, TestSubscriber>());

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        [Fact]
        public void AddTransientSubscriberWithGenericArguments_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddTransientSubscriber_TypeAndBaseTypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddTransientSubscriber(
                        typeof(ITestSubscriber),
                        typeof(TestSubscriber),
                        _ => new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddTransientSubscriberWithGenericArguments_TypeAndBaseTypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddTransientSubscriber<ITestSubscriber, TestSubscriber>(_ => new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddScopedSubscriber_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddScopedSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber)));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddScopedSubscriber_Type_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddScopedSubscriberWithGenericArguments_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddScopedSubscriber<ITestSubscriber, TestSubscriber>());

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddScopedSubscriber_TypeAndBaseTypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddScopedSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber), _ => new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddScopedSubscriberWithGenericArguments_TypeAndBaseTypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddScopedSubscriber<ITestSubscriber, TestSubscriber>(_ => new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddSingletonSubscriber_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber)));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddSingletonSubscriberWithGenericArguments_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber<ITestSubscriber, TestSubscriber>());

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddSingletonSubscriber_TypeAndBaseTypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber(
                        typeof(ITestSubscriber),
                        typeof(TestSubscriber),
                        _ => new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddSingletonSubscriberWithGenericArguments_TypeAndBaseTypeAndFactory_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber<ITestSubscriber, TestSubscriber>(_ => new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddSingletonSubscriber_TypeAndBaseTypeAndInstance_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber), new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
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
        public void AddSingletonSubscriberWithGenericArguments_TypeAndBaseTypeAndInstance_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber<ITestSubscriber, TestSubscriber>(new TestSubscriber()));

            serviceProvider.GetRequiredService<IBusConfigurator>()
                .Subscribe<ITestSubscriber>();

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
            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddSingletonSubscriber(new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        private static IServiceProvider GetServiceProvider(Action<IServiceCollection> configAction)
        {
            var services = new ServiceCollection()
                .AddNullLogger();

            configAction(services);

            return services.BuildServiceProvider();
        }
    }
}
