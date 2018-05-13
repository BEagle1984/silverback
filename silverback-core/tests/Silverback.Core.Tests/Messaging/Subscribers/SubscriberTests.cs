using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Tests.TestTypes.Domain;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class SubscriberTests
    {
        private TestCommandOneSubscriber _subscriber;

        [SetUp]
        public void Setup()
        {
            _subscriber = new TestCommandOneSubscriber();
        }

        [Test]
        public async Task BasicTest()
        {
            await _subscriber.OnNextAsync(new TestCommandOne());
            _subscriber.OnNext(new TestCommandOne());

            Assert.That(_subscriber.Counter, Is.EqualTo(2));
        }

        [Test]
        public void TypeFilteringTest()
        {
            _subscriber.OnNext(new TestCommandOne());
            _subscriber.OnNext(new TestCommandTwo());
            _subscriber.OnNext(new TestCommandOne());

            Assert.That(_subscriber.Counter, Is.EqualTo(2));
        }

        [Test]
        public void CustomFilteringTest()
        {
            _subscriber.Filter = m => m.Message == "yes";

            _subscriber.OnNext(new TestCommandOne { Message = "no" });
            _subscriber.OnNext(new TestCommandOne { Message = "yes" });
            _subscriber.OnNext(new TestCommandOne { Message = "yes" });

            Assert.That(_subscriber.Counter, Is.EqualTo(2));
        }
    }
}