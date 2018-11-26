// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Messages;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Publishing
{
    [TestFixture]
    public class PublisherTests
    {
        private TestSubscriber _syncSubscriber;
        private TestAsyncSubscriber _asyncSubscriber;

        [SetUp]
        public void Setup()
        {
            _syncSubscriber = new TestSubscriber();
            _asyncSubscriber = new TestAsyncSubscriber();
        }

        [Test]
        public void Publish_SomeMessages_Received()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(_syncSubscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishAsync_SomeMessages_Received()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(_syncSubscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void Publish_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(_syncSubscriber, _asyncSubscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

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
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(_syncSubscriber, _asyncSubscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

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
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(_syncSubscriber, _asyncSubscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

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
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(service1, service2), NullLoggerFactory.Instance.CreateLogger<Publisher>());

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
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new RepublishMessagesTestService(), service1, service2), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            publisher.Publish(message);

            Assert.That(service1.ReceivedMessagesCount, Is.EqualTo(expectedEventOne * 2));
            Assert.That(service2.ReceivedMessagesCount, Is.EqualTo(expectedEventTwo * 2));
        }

        [Test, TestCaseSource(nameof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestCases))]
        public async Task PublishAsync_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new RepublishMessagesTestService(), service1, service2), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(message);

            Assert.That(service1.ReceivedMessagesCount, Is.EqualTo(expectedEventOne * 2));
            Assert.That(service2.ReceivedMessagesCount, Is.EqualTo(expectedEventTwo * 2));
        }

        [Test]
        public void Publish_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestExceptionSubscriber()), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            Assert.Throws<AggregateException>(() => publisher.Publish(new TestEventOne()));
            Assert.Throws<AggregateException>(() => publisher.Publish(new TestEventTwo()));
        }

        [Test]
        public void PublishAsync_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestExceptionSubscriber()), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            Assert.ThrowsAsync<TargetInvocationException>(() => publisher.PublishAsync(new TestEventOne()));
            Assert.ThrowsAsync<TargetInvocationException>(() => publisher.PublishAsync(new TestEventTwo()));
        }

        [Test]
        public void Publish_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestRepublisher(), subscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            publisher.Publish(new TestCommandOne());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishAsync_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestRepublisher(), subscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandOne());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void Publish_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestRepublisher(), subscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            publisher.Publish(new TestCommandTwo());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(3));
        }

        [Test]
        public async Task PublishAsync_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestRepublisher(), subscriber), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(subscriber.ReceivedMessagesCount, Is.EqualTo(3));
        }

        [Test]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestRequestReplier()), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            var results = publisher.Publish<string>(new TestRequestOne());

            Assert.That(results, Is.EqualTo(new[] { "response", "response2" }));
        }

        [Test]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            var publisher = new Publisher(TestServiceProvider.Create<ISubscriber>(new TestRequestReplier()), NullLoggerFactory.Instance.CreateLogger<Publisher>());

            var results = await publisher.PublishAsync<string>(new TestRequestOne());

            Assert.That(results, Is.EqualTo(new[] { "response", "response2" }));
        }
    }
}
