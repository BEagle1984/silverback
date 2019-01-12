// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Core.Tests.TestTypes.Messages;
using Silverback.Core.Tests.TestTypes.Subscribers;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.Messaging.Publishing
{
    [TestFixture]
    public class PublisherTests
    {
        private TestSubscriber _syncSubscriber;
        private TestAsyncSubscriber _asyncSubscriber;
        private IPublisher _publisher;

        [SetUp]
        public void Setup()
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

        [Test]
        public void Publish_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishAsync_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void Publish_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
            Assert.That(_asyncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
        }

        [Test]
        public async Task PublishAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
            Assert.That(_asyncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
        }

        [Test]
        public async Task PublishSyncAndAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
            Assert.That(_asyncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
        }

        [Test]
        public async Task Publish_SomeMessages_ReceivedByAllSubscribedMethods()
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(service1, service2);

            await publisher.PublishAsync(new TestCommandOne());         // service1 +2
            await publisher.PublishAsync(new TestCommandTwo());         // service2 +2
            publisher.Publish(new TestCommandOne());                    // service1 +2
            await publisher.PublishAsync(new TransactionCommitEvent()); // service1/2 +1
            publisher.Publish(new TransactionRollbackEvent());          // service1/2 +1

            Assert.That(service1.ReceivedMessagesCount, Is.EqualTo(6));
            Assert.That(service2.ReceivedMessagesCount, Is.EqualTo(4));
        }

        public static IEnumerable<TestCaseData> Publish_SubscribedMessage_ReceivedRepublishedMessages_TestCases
        {
            get
            {
                yield return new TestCaseData(new TestEventOne(), 1, 0);
                yield return new TestCaseData(new TestEventTwo(), 1, 1);
            }
        }

        [Test, TestCaseSource(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestCases))]
        public void Publish_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(service1, service2);

            publisher.Publish(message);

            Assert.That(service1.ReceivedMessagesCount, Is.EqualTo(expectedEventOne * 2));
            Assert.That(service2.ReceivedMessagesCount, Is.EqualTo(expectedEventTwo * 2));
        }

        [Test, TestCaseSource(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestCases))]
        public async Task PublishAsync_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(service1, service2);

            await publisher.PublishAsync(message);

            Assert.That(service1.ReceivedMessagesCount, Is.EqualTo(expectedEventOne * 2));
            Assert.That(service2.ReceivedMessagesCount, Is.EqualTo(expectedEventTwo * 2));
        }

        [Test]
        public void Publish_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            Assert.Throws<AggregateException>(() => publisher.Publish(new TestEventOne()));
            Assert.Throws<AggregateException>(() => publisher.Publish(new TestEventTwo()));
        }

        [Test]
        public void PublishAsync_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            Assert.ThrowsAsync<TargetInvocationException>(() => publisher.PublishAsync(new TestEventOne()));
            Assert.ThrowsAsync<TargetInvocationException>(() => publisher.PublishAsync(new TestEventTwo()));
        }

        [Test]
        public void Publish_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandOne());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishAsync_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void Publish_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandTwo());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(3));
        }

        [Test]
        public async Task PublishAsync_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(3));
        }

        [Test]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRepublisher());

            var results = publisher.Publish<string>(new TestRequestCommandOne());

            Assert.That(results, Is.EqualTo(new[] { "response", "response2" }));
        }

        [Test]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRepublisher());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            Assert.That(results, Is.EqualTo(new[] { "response", "response2" }));
        }

        // TODO: Test new cases (parallel etc.)
    }
}
