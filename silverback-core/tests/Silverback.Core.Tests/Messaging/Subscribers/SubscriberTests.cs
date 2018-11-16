// TODO: DELETE

//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using NUnit.Framework;
//using Silverback.Tests.TestTypes.Messages;
//using Silverback.Tests.TestTypes.Subscribers;

//namespace Silverback.Tests.Messaging.Subscribers
//{
//    [TestFixture]
//    public class SubscriberTests
//    {
//        private TestSubscriber _subscriber;

//        [SetUp]
//        public void Setup()
//        {
//            _subscriber = new TestSubscriber(NullLoggerFactory.Instance.CreateLogger<TestSubscriber>());
//        }

//        [Test]
//        public void BasicTest()
//        {
//            _subscriber.OnMessageReceived(new TestCommandOne());

//            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
//        }

//        [Test]
//        public void TypeFilteringTest()
//        {
//            _subscriber.OnMessageReceived(new TestCommandOne());
//            _subscriber.OnMessageReceived(new TestCommandTwo());
//            _subscriber.OnMessageReceived(new TestCommandOne());

//            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(3));
//        }

//        [Test]
//        public void CustomFilteringTest()
//        {
//            _subscriber.Filter = m => m.Message == "yes";

//            _subscriber.OnMessageReceived(new TestCommandOne { Message = "no" });
//            _subscriber.OnMessageReceived(new TestCommandOne { Message = "yes" });
//            _subscriber.OnMessageReceived(new TestCommandOne { Message = "yes" });

//            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
//        }
//    }
//}