using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
{
    [TestFixture]
    public class OutboudConnectorTests
    {
        private OutboundConnector _connector;
        private OutboundRoutingConfiguration _routingConfiguration;
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker();
            _routingConfiguration = new OutboundRoutingConfiguration();
            _connector = new OutboundConnector(_broker, _routingConfiguration);
        }

        [Test]
        public async Task OnMessageReceived_SingleMessage_Relayed()
        {
            var endpoint = TestEndpoint.Default;

            _routingConfiguration.Add<IIntegrationMessage>(endpoint);
            var message = new TestEventOne { Content = "Test" };

            await _connector.OnMessageReceived(message);

            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(1));
            Assert.That(_broker.ProducedMessages.First().Endpoint, Is.EqualTo(endpoint));
            Assert.That(endpoint.Serializer.Deserialize(_broker.ProducedMessages.First().Message).Message.Id, Is.EqualTo(message.Id));
        }

        public static IEnumerable<TestCaseData> OnMessageReceived_MultipleMessages_CorrectlyRouted_TestCases
        {
            get
            {
                yield return new TestCaseData(new TestEventOne(), new[] {"allMessages", "allEvents", "eventOne"});
                yield return new TestCaseData(new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" });
            }
        }

        [Test]
        [TestCaseSource(nameof(OnMessageReceived_MultipleMessages_CorrectlyRouted_TestCases))]
        public async Task OnMessageReceived_MultipleMessages_CorrectlyRouted(IIntegrationMessage message, string[] expectedEndpointNames)
        {
            _routingConfiguration.Add<IIntegrationMessage>(new TestEndpoint("allMessages"));
            _routingConfiguration.Add<IIntegrationEvent>(new TestEndpoint("allEvents"));
            _routingConfiguration.Add<TestEventOne>(new TestEndpoint("eventOne"));
            _routingConfiguration.Add<TestEventTwo>(new TestEndpoint("eventTwo"));

            await _connector.OnMessageReceived(message);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                Assert.That(_broker.ProducedMessages.Count(x => x.Endpoint.Name == expectedEndpointName), Is.EqualTo(1));
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(r => r.DestinationEndpoint.Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                Assert.That(_broker.ProducedMessages.Count(x => x.Endpoint.Name == notExpectedEndpointName), Is.EqualTo(0));
            }
        }
    }
}