using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class MultiSubscriberTest
    {
        private TestEventsMultiSubscriber _subscriber;

        [SetUp]
        public void Setup()
        {
            _subscriber = new TestEventsMultiSubscriber();
        }

        [Test]
        public void RoutingTest()
        {
            _subscriber.OnNext(new TestEventOne());
            _subscriber.OnNext(new TestEventTwo());
            _subscriber.OnNext(new TestEventOne());
            _subscriber.OnNext(new TestEventOne());
            _subscriber.OnNext(new TestEventTwo());

            Assert.That(_subscriber.CounterEventOne, Is.EqualTo(3));
            Assert.That(_subscriber.CounterEventTwo, Is.EqualTo(2));
        }

        [Test]
        public void FilteringTest()
        {
            _subscriber.OnNext(new TestEventOne { Message = "yes" });
            _subscriber.OnNext(new TestEventOne { Message = "no" });
            _subscriber.OnNext(new TestEventOne { Message = "yes" });
            _subscriber.OnNext(new TestEventTwo { Message = "no" });
            _subscriber.OnNext(new TestEventTwo { Message = "no" });
            _subscriber.OnNext(new TestEventTwo { Message = "yes" });

            Assert.That(_subscriber.CounterFilteredOne, Is.EqualTo(2));
            Assert.That(_subscriber.CounterFilteredTwo, Is.EqualTo(1));
        }
    }
}
