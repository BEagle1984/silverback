using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
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
            _syncSubscriber = new TestSubscriber(NullLoggerFactory.Instance.CreateLogger<TestSubscriber>());
            _asyncSubscriber = new TestAsyncSubscriber(NullLoggerFactory.Instance.CreateLogger<TestAsyncSubscriber>());
        }

        [Test]
        public void PublishTest()
        {
            var publisher = new Publisher(new[] { _syncSubscriber }, NullLoggerFactory.Instance.CreateLogger<Publisher>());

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishAsyncTest()
        {
            var publisher = new Publisher(new[] { _syncSubscriber }, NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void MultipleSubscribersTest()
        {
            var publisher = new Publisher(new ISubscriber[] { _syncSubscriber, _asyncSubscriber }, NullLoggerFactory.Instance.CreateLogger<Publisher>());

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
            Assert.That(_asyncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
        }

        [Test]
        public async Task MultipleSubscribersAsyncTest()
        {
            var publisher = new Publisher(new ISubscriber[] { _syncSubscriber, _asyncSubscriber }, NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
            Assert.That(_asyncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
        }

        [Test]
        public async Task MultipleSubscribersSyncAndAsyncTest()
        {
            var publisher = new Publisher(new ISubscriber[] { _syncSubscriber, _asyncSubscriber }, NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            Assert.That(_syncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
            Assert.That(_asyncSubscriber.ReceivedMessagesCount, Is.EqualTo(5));
        }


        [Test]
        public async Task MultipleSubscribedMethodsTest()
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = new Publisher(new ISubscriber[] { service1, service2 }, NullLoggerFactory.Instance.CreateLogger<Publisher>());

            await publisher.PublishAsync(new TestCommandOne());         // service1 +2
            await publisher.PublishAsync(new TestCommandTwo());         // service2 +2
            publisher.Publish(new TestCommandOne());                    // service1 +2
            await publisher.PublishAsync(new TransactionCommitEvent()); // service1/2 +1
            publisher.Publish(new TransactionRollbackEvent());          // service1/2 +1

            Assert.That(service1.ReceivedMessagesCount, Is.EqualTo(6));
            Assert.That(service2.ReceivedMessagesCount, Is.EqualTo(4));
        }
    }
}
