using System.Threading.Tasks;
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

            Assert.That(subscriber.Counter, Is.EqualTo(2));
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
    }
}
