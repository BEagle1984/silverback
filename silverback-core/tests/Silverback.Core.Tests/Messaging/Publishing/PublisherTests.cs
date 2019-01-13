// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Core.Tests.TestTypes.Messages;
using Silverback.Core.Tests.TestTypes.Subscribers;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Xunit;

namespace Silverback.Core.Tests.Messaging.Publishing
{
    [Collection("Core.Messaging")]
    public class PublisherTests
    {
        private readonly TestSubscriber _syncSubscriber;
        private readonly TestAsyncSubscriber _asyncSubscriber;
        private IPublisher _publisher;

        public PublisherTests()
        {
            _syncSubscriber = new TestSubscriber();
            _asyncSubscriber = new TestAsyncSubscriber();
        }

        private IPublisher GetPublisher(params ISubscriber[] subscribers)
        {
            var services = new ServiceCollection();
            services.AddBus();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            foreach (var sub in subscribers)
                services.AddSingleton<ISubscriber>(sub);

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<IPublisher>();
        }

        [Fact]
        public void Publish_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task PublishSyncAndAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task Publish_SomeMessages_ReceivedByAllSubscribedMethods()
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(service1, service2);

            await publisher.PublishAsync(new TestCommandOne());         // service1 +2
            await publisher.PublishAsync(new TestCommandTwo());         // service2 +2
            publisher.Publish(new TestCommandOne());                    // service1 +2
            await publisher.PublishAsync(new TransactionCompleteEvent()); // service1/2 +1
            publisher.Publish(new TransactionAbortedEvent());          // service1/2 +1

            service1.ReceivedMessagesCount.Should().Be(6);
            service2.ReceivedMessagesCount.Should().Be(4);
        }

        [Theory, ClassData(typeof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public void Publish_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(new RepublishMessagesTestService(), service1, service2);

            publisher.Publish(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Theory, ClassData(typeof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public async Task PublishAsync_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(new RepublishMessagesTestService(), service1, service2);

            await publisher.PublishAsync(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Fact]
        public void Publish_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            publisher.Invoking(x => x.Publish(new TestEventOne())).Should().Throw<AggregateException>();
            publisher.Invoking(x => x.Publish(new TestEventTwo())).Should().Throw<AggregateException>();
        }

        [Fact]
        public void PublishAsync_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            Func<Task> act1 = async () => await publisher.PublishAsync(new TestEventOne());
            Func<Task> act2 = async () => await publisher.PublishAsync(new TestEventTwo());

            act1.Should().Throw<AggregateException>();
            act2.Should().Throw<AggregateException>();
        }

        [Fact]
        public void Publish_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRequestReplier());

            var results = publisher.Publish<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRequestReplier());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        //public void Publish_


        /* TODO: Implement following tests:
         * - Subscriber with ienumerable as input
         * - Publish in batch
         * - Parallel
         * - Exclusive
         * - Parallel and Exclusive
         * - Static subscriber
         * - Additional arguments */
    }
}
