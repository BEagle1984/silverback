using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Tests.TestTypes.Messages;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class SubscriberTests
    {
        private TestSubscriber _subscriber;

        [SetUp]
        public void Setup()
        {
            _subscriber = new TestSubscriber(NullLoggerFactory.Instance.CreateLogger<TestSubscriber>());
        }

        [Test]
        public void BasicTest()
        {
            _subscriber.OnNext(new TestCommandOne());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
        }

        [Test]
        public void TypeFilteringTest()
        {
            _subscriber.OnNext(new TestCommandOne());
            _subscriber.OnNext(new TestCommandTwo());
            _subscriber.OnNext(new TestCommandOne());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(3));
        }

        [Test]
        public void CustomFilteringTest()
        {
            _subscriber.Filter = m => m.Message == "yes";

            _subscriber.OnNext(new TestCommandOne { Message = "no" });
            _subscriber.OnNext(new TestCommandOne { Message = "yes" });
            _subscriber.OnNext(new TestCommandOne { Message = "yes" });

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }
    }
}