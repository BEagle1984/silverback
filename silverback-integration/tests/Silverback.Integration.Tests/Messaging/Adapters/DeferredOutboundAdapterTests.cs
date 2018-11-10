using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Repositories;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class DeferredOutboundAdapterTests
    {
        private InMemoryOutboundQueue _queue;
        private DeferredOutboundAdapter _adapter;

        [SetUp]
        public void Setup()
        {
            _queue = new InMemoryOutboundQueue();
            _queue.Clear();
            _adapter = new DeferredOutboundAdapter(_queue);
        }

        [Test]
        public void RelayTest()
        {
            var message = new TestEventOne { Content = "Test" };
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");

            _adapter.Relay(message, null, endpoint);

            _adapter.OnTransactionCommit(null);

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

            await _adapter.RelayAsync(message, null, endpoint);

            _adapter.OnTransactionCommit(null);

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

            _adapter.Relay(message, null, endpoint);

            _adapter.OnTransactionRollback(null);

            Assert.That(_queue.Length, Is.EqualTo(0));
        }
    }
}
