using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging
{
    [TestFixture]
    public class BusTests
    {
        [Test]
        public void PublishSubscribeBasicTest()
        {
            using (var bus = new Bus())
            {
                var subscriber = (TestSubscriber)bus.Subscribe(new TestSubscriber());

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());

                Assert.That(subscriber.Handled, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task PublishAsyncTest()
        {
            using (var bus = new Bus())
            {
                var subscriber = (TestAsyncSubscriber)bus.Subscribe(new TestAsyncSubscriber());

                await bus.PublishAsync(new TestCommandOne());
                await bus.PublishAsync(new TestCommandTwo());

                Assert.That(subscriber.Handled, Is.EqualTo(2));
            }
        }

        [Test]
        public void MultipleSubscribersTest()
        {
            using (var bus = new Bus())
            {
                var subscriber1 = (TestSubscriber)bus.Subscribe(new TestSubscriber());
                var subscriber2 = (TestAsyncSubscriber)bus.Subscribe(new TestAsyncSubscriber());

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(subscriber1.Handled, Is.EqualTo(5));
                Assert.That(subscriber2.Handled, Is.EqualTo(5));
            }
        }

        [Test]
        public async Task MultipleSubscribersAsyncTest()
        {
            using (var bus = new Bus())
            {
                var subscriber1 = (TestSubscriber)bus.Subscribe(new TestSubscriber());
                var subscriber2 = (TestAsyncSubscriber)bus.Subscribe(new TestAsyncSubscriber());

                await bus.PublishAsync(new TestCommandOne());
                await bus.PublishAsync(new TestCommandTwo());
                await bus.PublishAsync(new TestCommandOne());
                await bus.PublishAsync(new TestCommandTwo());
                await bus.PublishAsync(new TestCommandTwo());

                Assert.That(subscriber1.Handled, Is.EqualTo(5));
                Assert.That(subscriber2.Handled, Is.EqualTo(5));
            }
        }

        [Test]
        public void UnsubscribeTest()
        {
            using (var bus = new Bus())
            {
                var subscriber1 = (TestAsyncSubscriber)bus.Subscribe(new TestAsyncSubscriber());
                var subscriber2 = (TestSubscriber)bus.Subscribe(new TestSubscriber());

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());

                bus.Unsubscribe(subscriber1);

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());
                bus.Publish(new TestCommandTwo());

                Assert.That(subscriber1.Handled, Is.EqualTo(2));
                Assert.That(subscriber2.Handled, Is.EqualTo(5));
            }
        }

        [Test]
        public void DisposeTest()
        {
            var subscriber1 = new TestAsyncSubscriber();
            var subscriber2 = new TestSubscriber();

            using (var bus = new Bus())
            {
                bus.Subscribe(subscriber1);
                bus.Subscribe(subscriber2);

                bus.Publish(new TestCommandOne());
                bus.Publish(new TestCommandTwo());

                Assert.That(subscriber1.Disposed, Is.EqualTo(false));
                Assert.That(subscriber1.Disposed, Is.EqualTo(false));
            }

            Assert.That(subscriber1.Disposed, Is.EqualTo(true));
            Assert.That(subscriber1.Disposed, Is.EqualTo(true));
        }
    }
}
