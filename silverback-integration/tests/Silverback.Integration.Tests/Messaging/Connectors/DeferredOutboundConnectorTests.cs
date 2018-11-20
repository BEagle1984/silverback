using System.Collections.Generic;
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
        private OutboundRoutingConfiguration _routingConfiguration;

        [SetUp]
        public void Setup()
        {
            _queue = new InMemoryOutboundQueue();
            _routingConfiguration = new OutboundRoutingConfiguration();
            _connector = new DeferredOutboundConnector(_queue, _routingConfiguration);

            InMemoryOutboundQueue.Clear();
        }

        [Test]
        public async Task OnMessageReceived_SingleMessage_Enqueued()
        {
            var endpoint = TestEndpoint.Default;

            _routingConfiguration.Add<IIntegrationMessage>(endpoint);
            var message = new TestEventOne { Content = "Test" };

            await _connector.OnMessageReceived(message);
            await _queue.Commit();

            Assert.That(_queue.Length, Is.EqualTo(1));
            var enqueued = _queue.Dequeue(1).First();
            Assert.That(enqueued.Endpoint, Is.EqualTo(endpoint));
            Assert.That(enqueued.Message.Id, Is.EqualTo(message.Id));
        }

        public static IEnumerable<TestCaseData> OnMessageReceived_MultipleMessages_CorrectlyRouted_TestCases
        {
            get
            {
                yield return new TestCaseData(new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" });
                yield return new TestCaseData(new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" });
            }
        }

        [Test]
        [TestCaseSource(nameof(OnMessageReceived_MultipleMessages_CorrectlyRouted_TestCases))]
        public async Task OnMessageReceived_MultipleMessages_CorrectlyRouted(IIntegrationMessage message, string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(TestEndpoint.Create("allMessages"));
            _routingConfiguration.Add<IIntegrationEvent>(TestEndpoint.Create("allEvents"));
            _routingConfiguration.Add<TestEventOne>(TestEndpoint.Create("eventOne"));
            _routingConfiguration.Add<TestEventTwo>(TestEndpoint.Create("eventTwo"));

            await _connector.OnMessageReceived(message);
            await _queue.Commit();

            var enqueued = _queue.Dequeue(100);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                Assert.That(enqueued.Count(x => x.Endpoint.Name == expectedEndpointName), Is.EqualTo(1));
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(r => r.DestinationEndpoint.Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                Assert.That(enqueued.Count(x => x.Endpoint.Name == notExpectedEndpointName), Is.EqualTo(0));
            }
        }

        [Test]
        public void CommitRollback_ReceiveCommitReceiveRollback_FirstIsCommittedSecondIsDiscarded()
        {
            _routingConfiguration.Add<IIntegrationMessage>(TestEndpoint.Create("allMessages"));

            _connector.OnMessageReceived(new TestEventOne());
            _connector.OnTransactionCommit(new TransactionCommitEvent());
            _connector.OnMessageReceived(new TestEventOne());
            _connector.OnTransactionRollback(new TransactionRollbackEvent());

            Assert.That(_queue.Length, Is.EqualTo(1));
        }
    }
}
