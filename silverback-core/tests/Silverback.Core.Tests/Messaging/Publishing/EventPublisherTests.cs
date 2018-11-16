using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.TestTypes.Messages;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Publishing
{
    [TestFixture]
    public class EventPublisherTests
    {
        private IPublisher _publisher;
        private TestSubscriber _subscriber;

        [SetUp]
        public void Setup()
        {
            _subscriber = new TestSubscriber();
            _publisher = new Publisher(new[] { _subscriber }, NullLoggerFactory.Instance.CreateLogger<Publisher>());
        }

        [Test]
        public void PublishEventTest()
        {
            var publisher = new EventPublisher<IEvent>(_publisher);

            publisher.Publish(new TestEventOne());
            publisher.Publish(new TestEventTwo());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void PublishSpecificEventTest()
        {
            var publisher = new EventPublisher<TestEventOne>(_publisher);

            publisher.Publish(new TestEventOne());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
        }

        [Test]
        public async Task PublishEventAsyncTest()
        {
            var publisher = new EventPublisher<IEvent>(_publisher);

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishSpecificEventAsyncTest()
        {
            var publisher = new EventPublisher<TestEventTwo>(_publisher);

            await publisher.PublishAsync(new TestEventTwo());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
        }
    }
}