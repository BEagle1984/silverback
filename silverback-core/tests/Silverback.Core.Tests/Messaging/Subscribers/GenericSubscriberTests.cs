using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class GenericSubscriberTests
    {
        private GenericSubscriber<IMessage> _subscriber;
        private int _counter;

        [SetUp]
        public void Setup()
        {
            _counter = 0;
            _subscriber = new GenericSubscriber<IMessage>(m => _counter++);
        }

        [Test]
        public void BasicTest()
        {
            _subscriber.OnNext(new TestCommandOne());
            _subscriber.OnNext(new TestCommandTwo());

            Assert.That(_counter, Is.EqualTo(2));
        }
    }
}