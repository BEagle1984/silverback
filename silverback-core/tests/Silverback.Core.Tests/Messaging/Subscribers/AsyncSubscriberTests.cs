using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Tests.TestTypes.Messages;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class AsyncSubscriberTests
    {
        private TestAsyncSubscriber _subscriber;

        [SetUp]
        public void Setup()
        {
            _subscriber = new TestAsyncSubscriber(NullLoggerFactory.Instance.CreateLogger<TestAsyncSubscriber>());
        }

        [Test]
        public async Task BasicTest()
        {
            await _subscriber.OnNextAsync(new TestCommandOne());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CustomFilteringTest()
        {
            _subscriber.Filter = m => m.Message == "yes";

            await _subscriber.OnNextAsync(new TestCommandOne { Message = "no" });
            await _subscriber.OnNextAsync(new TestCommandOne { Message = "yes" });
            await _subscriber.OnNextAsync(new TestCommandOne { Message = "yes" });

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }
    }
}