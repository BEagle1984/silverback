using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Subscribers
{
    [TestFixture]
    public class GenericAsyncSubscriberTests
    {
        private GenericAsyncSubscriber<IMessage> _subscriber;
        private int _counter;

        [SetUp]
        public void Setup()
        {
            _counter = 0;
            _subscriber = new GenericAsyncSubscriber<IMessage>(async m =>
            {
                await Task.Delay(1);
                _counter++;
            });
        }

        [Test]
        public async Task BasicTest()
        {
            await _subscriber.OnNextAsync(new TestCommandOne());
            await _subscriber.OnNextAsync(new TestCommandTwo());

            Assert.That(_counter, Is.EqualTo(2));
        }
    }
}