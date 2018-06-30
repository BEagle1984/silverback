using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class SubscriberFactoryTests
    {
        [Test]
        public async Task BasicTest()
        {
            var subscriber = new TestSubscriber();
            var subscriberFactory = new SubscriberFactory<TestSubscriber>(new GenericTypeFactory(t => subscriber));

            subscriberFactory.OnNext(new TestCommandOne());
            await subscriberFactory.OnNextAsync(new TestCommandTwo());

            Assert.That(subscriber.Handled, Is.EqualTo(2));
        }

        [Test]
        public async Task AsyncSubscriberTest()
        {
            var asyncSubscriber = new TestAsyncSubscriber();
            var subscriberFactory = new SubscriberFactory<TestAsyncSubscriber>(new GenericTypeFactory(t => asyncSubscriber));

            subscriberFactory.OnNext(new TestCommandOne());
            await subscriberFactory.OnNextAsync(new TestCommandTwo());

            Assert.That(asyncSubscriber.Handled, Is.EqualTo(2));
        }

        [Test]
        public async Task MultipleSubscribersTest()
        {
            var subscriber1 = new TestSubscriber();
            var subscriber2 = new TestSubscriber();
            var subscriberFactory = new SubscriberFactory<ISubscriber>(
                new GenericTypeFactory(t => new object[] { subscriber1, subscriber2 }));

            subscriberFactory.OnNext(new TestCommandOne());
            await subscriberFactory.OnNextAsync(new TestCommandTwo());

            Assert.That(subscriber1.Handled, Is.EqualTo(2));
            Assert.That(subscriber2.Handled, Is.EqualTo(2));
        }

        [Test]
        public async Task MultipleAsyncSubscribersTest()
        {
            var subscriber1 = new TestAsyncSubscriber();
            var subscriber2 = new TestAsyncSubscriber();
            var subscriberFactory = new SubscriberFactory<ISubscriber>(
                new GenericTypeFactory(t => new object[] { subscriber1, subscriber2 }));

            subscriberFactory.OnNext(new TestCommandOne());
            await subscriberFactory.OnNextAsync(new TestCommandTwo());

            Assert.That(subscriber1.Handled, Is.EqualTo(2));
            Assert.That(subscriber2.Handled, Is.EqualTo(2));
        }
    }
}
