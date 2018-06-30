using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
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

            Assert.That(_subscriber.HandledEventOne, Is.EqualTo(3));
            Assert.That(_subscriber.HandledEventTwo, Is.EqualTo(2));
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

            Assert.That(_subscriber.HandledFilteredEventOne, Is.EqualTo(2));
            Assert.That(_subscriber.HandledFilteredEventTwo, Is.EqualTo(1));
        }
    }
}
