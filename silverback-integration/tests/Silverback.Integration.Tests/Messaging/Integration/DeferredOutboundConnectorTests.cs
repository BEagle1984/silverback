using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Integration.Repositories;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Integration
{
    [TestFixture]
    public class DeferredOutboundConnectorTests
    {
        private InMemoryOutboundQueue _queue;
        private DeferredOutboundConnector _connector;

        [SetUp]
        public void Setup()
        {
            _queue = new InMemoryOutboundQueue();
            _queue.Clear();
            _connector = new DeferredOutboundConnector(_queue);
        }

        [Test]
        public void RelayTest()
        {
            var message = new TestEventOne { Content = "Test" };
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");

            _connector.Relay(message, null, endpoint);

            _connector.OnTransactionCommit(null);

            Assert.That(_queue.Length, Is.EqualTo(1));
            var item =_queue.Dequeue(1).First();
            Assert.That(item.Message, Is.EqualTo(message));
            Assert.That(item.Endpoint, Is.EqualTo(endpoint));
        }
        
        [Test]
        public async Task RelayAsyncTest()
        {
            var message = new TestEventOne { Content = "Test" };
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");

            await _connector.RelayAsync(message, null, endpoint);

            _connector.OnTransactionCommit(null);

            Assert.That(_queue.Length, Is.EqualTo(1));
            var item = _queue.Dequeue(1).First();
            Assert.That(item.Message, Is.EqualTo(message));
            Assert.That(item.Endpoint, Is.EqualTo(endpoint));
        }

        [Test]
        public void RollbackTest()
        {
            var message = new TestEventOne { Content = "Test" };
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");

            _connector.Relay(message, null, endpoint);

            _connector.OnTransactionRollback(null);

            Assert.That(_queue.Length, Is.EqualTo(0));
        }
    }
}
