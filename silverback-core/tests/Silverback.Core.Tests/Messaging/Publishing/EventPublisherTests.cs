using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class EventPublisherTests
    {
        [Test]
        public void PublishEventTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Config().Subscribe(m => counter++);

                var publisher = bus.GetEventPublisher<IEvent>();

                publisher.Publish(new TestEventOne());
                publisher.Publish(new TestEventTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public void PublishSpecificEventTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Config().Subscribe(m => counter++);

                var publisher = bus.GetEventPublisher<TestEventOne>();

                publisher.Publish(new TestEventOne());

                Assert.That(counter, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task PublishEventAsyncTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Config().Subscribe(m => counter++);

                var publisher = bus.GetEventPublisher<IEvent>();

                await publisher.PublishAsync(new TestEventOne());
                await publisher.PublishAsync(new TestEventTwo());

                Assert.That(counter, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task PublishSpecificEventAsyncTest()
        {
            using (var bus = new Bus())
            {
                var counter = 0;
                bus.Config().Subscribe(m => counter++);

                var publisher = bus.GetEventPublisher<TestEventOne>();

                await publisher.PublishAsync(new TestEventOne());

                Assert.That(counter, Is.EqualTo(1));
            }
        }
    }
}