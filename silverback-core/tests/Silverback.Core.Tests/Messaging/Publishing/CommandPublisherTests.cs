using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;
using Silverback.Tests.TestTypes.Subscribers;

namespace Silverback.Tests.Messaging.Publishing
{
    [TestFixture]
    public class CommandPublisherTests
    {
        private IPublisher _publisher;
        private TestSubscriber _subscriber;

        [SetUp]
        public void Setup()
        {
            _subscriber = new TestSubscriber();
            _publisher = new Publisher(new[] {_subscriber}, NullLoggerFactory.Instance.CreateLogger<Publisher>());
        }

        [Test]
        public void SendCommandTest()
        {
            var publisher = new CommandPublisher<ICommand>(_publisher);

            publisher.Send(new TestCommandOne());
            publisher.Send(new TestCommandTwo());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public void SendSpecificCommandTest()
        {
            var publisher = new CommandPublisher<TestCommandOne>(_publisher);

            publisher.Send(new TestCommandOne());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
        }

        [Test]
        public async Task SendCommandAsyncTest()
        {
            var publisher = new CommandPublisher<ICommand>(_publisher);

            await publisher.SendAsync(new TestCommandOne());
            await publisher.SendAsync(new TestCommandTwo());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(2));
        }

        [Test]
        public async Task SendSpecificCommandAsyncTest()
        {
            var publisher = new CommandPublisher<TestCommandTwo>(_publisher);

            await publisher.SendAsync(new TestCommandTwo());

            Assert.That(_subscriber.ReceivedMessagesCount, Is.EqualTo(1));
        }
    }
}