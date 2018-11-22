using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
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
            _connector = new DeferredOutboundConnector(_queue);

            InMemoryOutboundQueue.Clear();
        }

        [Test]
        public async Task OnMessageReceived_SingleMessage_Enqueued()
        {
            var endpoint = TestEndpoint.Default;

            var message = new TestEventOne { Content = "Test" };

            await _connector.RelayMessage(message, endpoint);
            await _queue.Commit();

            Assert.That(_queue.Length, Is.EqualTo(1));
            var enqueued = _queue.Dequeue(1).First();
            Assert.That(enqueued.Endpoint, Is.EqualTo(endpoint));
            Assert.That(enqueued.Message.Id, Is.EqualTo(message.Id));
        }

        [Test]
        public void CommitRollback_ReceiveCommitReceiveRollback_FirstIsCommittedSecondIsDiscarded()
        {
            _connector.RelayMessage(new TestEventOne(), TestEndpoint.Default);
            _connector.OnTransactionCommit(new TransactionCommitEvent());
            _connector.RelayMessage(new TestEventOne(), TestEndpoint.Default);
            _connector.OnTransactionRollback(new TransactionRollbackEvent());

            Assert.That(_queue.Length, Is.EqualTo(1));
        }
    }
}
