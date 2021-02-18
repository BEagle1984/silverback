// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Logging;
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            static TestEventTwo ReceiveOne(TestEventOne message) => new();
            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, object>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventOne());

            received.Should().Be(2);
        }

        [Fact]
        public async Task AddDelegateSubscriber_FuncOfEnumerableReturningMessage_MessagesRepublished()
        {
            int received = 0;

            static TestEventTwo ReceiveOne(IEnumerable<TestEventOne> messages) => new();
            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, object>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());

            received.Should().Be(2);
        }

        [Fact]
        public async Task AddDelegateSubscriber_FuncOfEnumerableReturningTaskWithMessage_MessagesRepublished()
        {
            int received = 0;

            static Task<object> ReceiveOne(IEnumerable<TestEventOne> messages) =>
                Task.FromResult<object>(new TestEventTwo());

            void ReceiveTwo(TestEventTwo message) => received++;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<IEnumerable<TestEventOne>, Task<object>>)ReceiveOne)
                    .AddDelegateSubscriber((Action<TestEventTwo>)ReceiveTwo));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());

            received.Should().Be(2);
        }

        [Fact]
        public void
            AddDelegateSubscriber_ActionWithServiceProvider_MessagesAndServiceProviderInstanceReceived()
        {
            int received = 0;

            void Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
            }

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((Action<TestEventOne, IServiceProvider>)Receive));

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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((Func<TestEventOne, IServiceProvider, Task>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }

        [Fact]
        public void
            AddDelegateSubscriber_FuncWithServiceProviderReturningMessage_ServiceProviderInstanceReceived()
        {
            int received = 0;

            TestEventTwo Receive(TestEventOne message, IServiceProvider provider)
            {
                received++;
                provider.Should().NotBeNull();
                return new TestEventTwo();
            }

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
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

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (Func<TestEventOne, IServiceProvider, Task<TestEventTwo>>)Receive));

            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            publisher.Publish(new TestEventOne());

            received.Should().BeGreaterThan(0);
        }
    }
}
