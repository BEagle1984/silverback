using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
{
    [TestFixture]
    public class OutboundConnectorRouterTests
    {
        private OutboundConnectorRouter _connectorRouter;
        private OutboundRoutingConfiguration _routingConfiguration;
        private InMemoryOutboundQueue _outboundQueue;
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker();

            _outboundQueue = new InMemoryOutboundQueue();

            var outboundConnector = new OutboundConnector(_broker);
            var deferredOutboundConnector = new DeferredOutboundConnector(_outboundQueue);

            _routingConfiguration = new OutboundRoutingConfiguration();
            _connectorRouter = new OutboundConnectorRouter(
                _routingConfiguration,
                new IOutboundConnector[]
                {
                    deferredOutboundConnector,
                    outboundConnector
                });

            InMemoryOutboundQueue.Clear();
        }

        public static IEnumerable<TestCaseData> OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoints_TestCases
        {
            get
            {
                yield return new TestCaseData(new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" });
                yield return new TestCaseData(new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" });
            }
        }

        [Test]
        [TestCaseSource(nameof(OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoints_TestCases))]
        public async Task OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoint(IIntegrationMessage message, string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(new TestEndpoint("allMessages"), null);
            _routingConfiguration.Add<IIntegrationEvent>(new TestEndpoint("allEvents"), null);
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), null);
            _routingConfiguration.Add<TestEventTwo>(new TestEndpoint("eventTwo"), null);

            await _connectorRouter.OnMessageReceived(message);
            await _outboundQueue.Commit();

            var enqueued = _outboundQueue.Dequeue(100);

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
        public async Task OnMessageReceived_Message_CorrectlyRoutedToDefaultConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), null);

            await _connectorRouter.OnMessageReceived(new TestEventOne());
            await _outboundQueue.Commit();

            var enqueued = _outboundQueue.Dequeue(1);
            Assert.That(enqueued.Count(), Is.EqualTo(1));
            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task OnMessageReceived_Message_CorrectlyRoutedToConnector()
        {
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"), typeof(OutboundConnector));

            await _connectorRouter.OnMessageReceived(new TestEventOne());
            await _outboundQueue.Commit();

            var enqueued = _outboundQueue.Dequeue(1);
            Assert.That(enqueued.Count(), Is.EqualTo(0));
            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(1));
        }
    }
}