// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class BusConfiguratorSubscribeExtensionsTests
    {
        private readonly IBusConfigurator _busConfigurator;

        private readonly IPublisher _publisher;

        private readonly TestSubscriber _testSubscriber;

        public BusConfiguratorSubscribeExtensionsTests()
        {
            var serviceProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .AddScopedSubscriber<ITestSubscriber, TestSubscriber>()
                .Services.BuildServiceProvider();

            _busConfigurator = serviceProvider.GetRequiredService<IBusConfigurator>();

            var scopedServiceProvider = serviceProvider.CreateScope().ServiceProvider;
            _publisher = scopedServiceProvider.GetRequiredService<IPublisher>();
            _testSubscriber = scopedServiceProvider.GetRequiredService<TestSubscriber>();
        }

        private delegate void ReceiveTestEventOneDelegate(TestEventOne message);

        [Fact]
        public void Subscribe_Delegate_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(TestEventOne message) => received++;

            _busConfigurator.Subscribe((ReceiveTestEventOneDelegate)Receive);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_Action_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(TestEventOne message) => received++;

            _busConfigurator.Subscribe((Action<TestEventOne>)Receive);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncReturningTask_MatchingMessagesReceived()
        {
            int received = 0;

            Task Receive(TestEventOne message)
            {
                received++;
                return Task.CompletedTask;
            }

            _busConfigurator.Subscribe((Func<TestEventOne, Task>)Receive);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncReturningMessage_MessagesRepublished()
        {
            int received = 0;

            static TestEventTwo ReceiveOne(TestEventOne message) => new TestEventTwo();
            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<TestEventOne, object>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncReturningEnumerableOfMessage_MessagesRepublished()
        {
            int received = 0;

            static IEnumerable<TestEventTwo> ReceiveOne(TestEventOne message) =>
                new[] { new TestEventTwo(), new TestEventTwo() };

            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<TestEventOne, IEnumerable<object>>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(4);
        }

        [Fact]
        public void Subscribe_FuncReturningCollectionOfMessage_MessagesRepublished()
        {
            int received = 0;

            static IReadOnlyCollection<TestEventTwo> ReceiveOne(TestEventOne message) =>
                new[] { new TestEventTwo(), new TestEventTwo() };

            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<TestEventOne, IReadOnlyCollection<object>>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(4);
        }

        [Fact]
        public void Subscribe_FuncReturningTaskWithMessage_MessagesRepublished()
        {
            int received = 0;

            static Task<TestEventTwo> ReceiveOne(TestEventOne message) => Task.FromResult(new TestEventTwo());
            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<TestEventOne, object>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new TestEventOne());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_ActionOfEnumerable_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(IEnumerable<TestEventOne> messages) => received += messages.Count();

            _busConfigurator.Subscribe((Action<IEnumerable<TestEventOne>>)Receive);

            _publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(3);
        }

        [Fact]
        public void Subscribe_ActionOfReadOnlyCollection_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(IReadOnlyCollection<TestEventOne> messages) => received += messages.Count;

            _busConfigurator.Subscribe((Action<IReadOnlyCollection<TestEventOne>>)Receive);

            _publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(3);
        }

        [Fact]
        public void Subscribe_FuncOfEnumerableReturningTask_MatchingMessagesReceived()
        {
            int received = 0;

            Task Receive(IEnumerable<TestEventOne> messages)
            {
                received += messages.Count();
                return Task.CompletedTask;
            }

            _busConfigurator.Subscribe((Func<IEnumerable<TestEventOne>, Task>)Receive);

            _publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            _publisher.Publish(new TestEventTwo());
            _publisher.Publish(new TestEventOne());

            received.Should().Be(3);
        }

        [Fact]
        public void Subscribe_FuncOfEnumerableReturningMessage_MessagesRepublished()
        {
            int received = 0;

            static TestEventTwo ReceiveOne(IEnumerable<TestEventOne> messages) => new TestEventTwo();
            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<IEnumerable<TestEventOne>, object>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_FuncOfEnumerableReturningTaskWithMessage_MessagesRepublished()
        {
            int received = 0;

            static Task<object> ReceiveOne(IEnumerable<TestEventOne> messages) =>
                Task.FromResult<object>(new TestEventTwo());

            void ReceiveTwo(TestEventTwo message) => received++;

            _busConfigurator.Subscribe((Func<IEnumerable<TestEventOne>, Task<object>>)ReceiveOne);
            _busConfigurator.Subscribe((Action<TestEventTwo>)ReceiveTwo);

            _publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            _publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void Subscribe_ActionWithServiceProvider_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            void Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
            }

            _busConfigurator.Subscribe((Action<TestEventOne, IServiceProvider>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Subscribe_ActionOfEnumerableWithServiceProvider_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            void Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
            }

            _busConfigurator.Subscribe((Action<IEnumerable<TestEventOne>, IServiceProvider>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Subscribe_FuncWithServiceProviderReturningTask_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            Task Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return Task.CompletedTask;
            }

            _busConfigurator.Subscribe((Func<TestEventOne, IServiceProvider, Task>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Subscribe_FuncWithServiceProviderReturningMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            TestEventTwo Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return new TestEventTwo();
            }

            _busConfigurator.Subscribe((Func<TestEventOne, IServiceProvider, TestEventTwo>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Subscribe_FuncWithServiceProviderReturningTaskWithMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            Task<TestEventTwo> Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return Task.FromResult(new TestEventTwo());
            }

            _busConfigurator.Subscribe((Func<TestEventOne, IServiceProvider, Task<TestEventTwo>>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            Subscribe_FuncOfEnumerableWithServiceProviderReturningTask_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            Task Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
                return Task.CompletedTask;
            }

            _busConfigurator.Subscribe((Func<IEnumerable<TestEventOne>, IServiceProvider, Task>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Subscribe_FuncOfEnumerableWithServiceProviderReturningMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            TestEventTwo Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
                return new TestEventTwo();
            }

            _busConfigurator.Subscribe((Func<IEnumerable<TestEventOne>, IServiceProvider, TestEventTwo>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            Subscribe_FuncOfEnumerableWithServiceProviderReturningTaskWithMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            Task<TestEventTwo> Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
                return Task.FromResult(new TestEventTwo());
            }

            _busConfigurator.Subscribe((Func<IEnumerable<TestEventOne>, IServiceProvider, Task<TestEventTwo>>)Receive);

            _publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Subscribe_Interface_MessagesReceived()
        {
            _busConfigurator.Subscribe<ITestSubscriber>();

            _publisher.Publish(new TestEventOne());

            _testSubscriber.ReceivedCallsCount.Should().Be(3);
        }

        [Fact]
        public void Subscribe_Type_MessagesReceived()
        {
            _busConfigurator.Subscribe<TestSubscriber>();

            _publisher.Publish(new TestEventOne());

            _testSubscriber.ReceivedCallsCount.Should().Be(3);
        }

        [Fact]
        public void Subscribe_AnnotatedMethodsOnly_MessagesReceived()
        {
            _busConfigurator.Subscribe<ITestSubscriber>(false);

            _publisher.Publish(new TestEventOne());

            _testSubscriber.ReceivedCallsCount.Should().Be(2);
        }
    }
}
