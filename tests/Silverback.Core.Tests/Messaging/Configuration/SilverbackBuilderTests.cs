// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class SilverbackBuilderTests
    {
        private IServiceProvider GetServiceProvider(Action<IServiceCollection> configAction)
        {
            var services = new ServiceCollection()
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            configAction(services);

            return services.BuildServiceProvider();
        }

        #region AddTransientSubscriber

        [Fact]
        public void AddTransientSubscriber_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber)));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientSubscriber<ITestSubscriber, TestSubscriber>());

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber), _ => new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientSubscriber<ITestSubscriber, TestSubscriber>(_ => new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientSubscriber<TestSubscriber>(_ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().Be(0); // It's hard to test the transient services
        }

        #endregion

        #region AddScopedSubscriber

        [Fact]
        public void AddScopedSubscriber_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber)));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedSubscriber<ITestSubscriber, TestSubscriber>());

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber), _ => new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedSubscriber<ITestSubscriber, TestSubscriber>(_ => new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedSubscriber<TestSubscriber>(_ => new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region AddSingletonSubscriber

        [Fact]
        public void AddSingletonSubscriber_TypeAndBaseType_SubscriberProperlyRegistered()
        {
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber)));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber<ITestSubscriber, TestSubscriber>());

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber), _ => new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber<ITestSubscriber, TestSubscriber>(_ => new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber<TestSubscriber>(_ => new TestSubscriber()));

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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber(typeof(ITestSubscriber), typeof(TestSubscriber), new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber<ITestSubscriber, TestSubscriber>(new TestSubscriber()));

            serviceProvider.GetRequiredService<BusConfigurator>()
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
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonSubscriber<TestSubscriber>(new TestSubscriber()));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            serviceProvider.GetRequiredService<TestSubscriber>()
                .ReceivedCallsCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region AddTransientBehavior

        [Fact]
        public void AddTransientBehavior_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientBehavior(typeof(ChangeTestEventOneContentBehavior)));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }
        
        [Fact]
        public void AddTransientBehaviorWithGenericArguments_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientBehavior<ChangeTestEventOneContentBehavior>());

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }
        
        [Fact]
        public void AddTransientBehavior_Factory_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddTransientBehavior(_ => new ChangeTestEventOneContentBehavior()));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        #endregion

        #region AddScopedBehavior

        [Fact]
        public void AddScopedBehavior_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedBehavior(typeof(ChangeTestEventOneContentBehavior)));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddScopedBehaviorWithGenericArguments_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedBehavior<ChangeTestEventOneContentBehavior>());

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddScopedBehavior_Factory_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddScopedBehavior(_ => new ChangeTestEventOneContentBehavior()));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        #endregion

        #region AddSingletonBehavior

        [Fact]
        public void AddSingletonBehavior_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonBehavior(typeof(ChangeTestEventOneContentBehavior)));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehaviorWithGenericArguments_Type_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonBehavior<ChangeTestEventOneContentBehavior>());

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehavior_Factory_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonBehavior(_ => new ChangeTestEventOneContentBehavior()));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        [Fact]
        public void AddSingletonBehavior_Instance_BehaviorProperlyRegistered()
        {
            var messages = new List<TestEventOne>();
            var serviceProvider = GetServiceProvider(services => services
                .AddSilverback()
                .AddSingletonBehavior(new ChangeTestEventOneContentBehavior()));

            serviceProvider.GetRequiredService<BusConfigurator>()
                .Subscribe<TestEventOne>(m => messages.Add(m));

            using var scope = serviceProvider.CreateScope();
            serviceProvider = scope.ServiceProvider;

            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());

            messages.ForEach(m => m.Message.Should().Be("behavior"));
        }

        #endregion
    }
}
