// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class SilverbackBuilderAddDelegateSubscriberExtensionsTests
    {
        private delegate void ReceiveTestEventOneDelegate(TestEventOne message);

        [Fact]
        public void AddDelegateSubscriber_Delegate_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(TestEventOne message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((ReceiveTestEventOneDelegate)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_Action_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(TestEventOne message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Action<TestEventOne>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncReturningTask_MatchingMessagesReceived()
        {
            int received = 0;

            Task Receive(TestEventOne message)
            {
                received++;
                return Task.CompletedTask;
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, Task>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncReturningMessage_MessagesRepublished()
        {
            int received = 0;

            static TestEventTwo ReceiveOne(TestEventOne message) => new TestEventTwo();
            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, object>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncReturningEnumerableOfMessage_MessagesRepublished()
        {
            int received = 0;

            static IEnumerable<TestEventTwo> ReceiveOne(TestEventOne message) =>
                new[] { new TestEventTwo(), new TestEventTwo() };

            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, IEnumerable<object>>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            received.Should().Be(4);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncReturningCollectionOfMessage_MessagesRepublished()
        {
            int received = 0;

            static IReadOnlyCollection<TestEventTwo> ReceiveOne(TestEventOne message) =>
                new[] { new TestEventTwo(), new TestEventTwo() };

            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, IReadOnlyCollection<object>>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            received.Should().Be(4);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncReturningTaskWithMessage_MessagesRepublished()
        {
            int received = 0;

            static Task<TestEventTwo> ReceiveOne(TestEventOne message) => Task.FromResult(new TestEventTwo());
            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, object>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_ActionOfEnumerable_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(IEnumerable<TestEventOne> messages) => received += messages.Count();

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Action<IEnumerable<TestEventOne>>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            received.Should().Be(3);
        }

        [Fact]
        public void AddDelegateSubscriber_ActionOfReadOnlyCollection_MatchingMessagesReceived()
        {
            int received = 0;

            void Receive(IReadOnlyCollection<TestEventOne> messages) => received += messages.Count;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Action<IReadOnlyCollection<TestEventOne>>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            received.Should().Be(3);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncOfEnumerableReturningTask_MatchingMessagesReceived()
        {
            int received = 0;

            Task Receive(IEnumerable<TestEventOne> messages)
            {
                received += messages.Count();
                return Task.CompletedTask;
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, Task>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            publisher.Publish(new TestEventTwo());
            publisher.Publish(new TestEventOne());

            received.Should().Be(3);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncOfEnumerableReturningMessage_MessagesRepublished()
        {
            int received = 0;

            static TestEventTwo ReceiveOne(IEnumerable<TestEventOne> messages) => new TestEventTwo();
            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, object>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncOfEnumerableReturningTaskWithMessage_MessagesRepublished()
        {
            int received = 0;

            static Task<object> ReceiveOne(IEnumerable<TestEventOne> messages) =>
                Task.FromResult<object>(new TestEventTwo());

            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, Task<object>>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new[] { new TestEventOne(), new TestEventOne() });
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public void AddDelegateSubscriber_ActionWithServiceProvider_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            void Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Action<TestEventOne, IServiceProvider>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_ActionOfEnumerableWithServiceProvider_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            void Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Action<IEnumerable<TestEventOne>, IServiceProvider>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_FuncWithServiceProviderReturningTask_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            Task Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return Task.CompletedTask;
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, Task>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AddDelegateSubscriber_FuncWithServiceProviderReturningMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            TestEventTwo Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return new TestEventTwo();
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, TestEventTwo>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_FuncWithServiceProviderReturningTaskWithMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            Task<TestEventTwo> Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return Task.FromResult(new TestEventTwo());
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, Task<TestEventTwo>>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_FuncOfEnumerableWithServiceProviderReturningTask_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            Task Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
                return Task.CompletedTask;
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, IServiceProvider, Task>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_FuncOfEnumerableWithServiceProviderReturningMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            TestEventTwo Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
                return new TestEventTwo();
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, IServiceProvider, TestEventTwo>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_FuncOfEnumerableWithServiceProviderReturningTaskWithMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            Task<TestEventTwo> Receive(IEnumerable<TestEventOne> messages, IServiceProvider provider)
            {
                received += messages.Count();
                provider.Should().NotBeNull();
                return Task.FromResult(new TestEventTwo());
            }

            var serviceProvider = GetServiceProvider(
                services => services
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (Func<IEnumerable<TestEventOne>, IServiceProvider, Task<TestEventTwo>>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        private static IServiceProvider GetServiceProvider(Action<IServiceCollection> configAction)
        {
            var services = new ServiceCollection()
                .AddNullLogger();

            configAction(services);

            return services.BuildServiceProvider().CreateScope().ServiceProvider;
        }
    }
}
