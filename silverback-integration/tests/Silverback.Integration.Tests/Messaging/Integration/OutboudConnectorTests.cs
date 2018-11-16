using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Integration
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
            _broker = new TestBroker(new JsonMessageSerializer());
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

            Assert.That(_broker.SentMessages.Count, Is.EqualTo(1));
            Assert.That(_broker.SentMessages.First().Endpoint, Is.EqualTo(endpoint));
            Assert.That(_broker.Serializer.Deserialize(_broker.SentMessages.First().Message).Message.Id, Is.EqualTo(message.Id));
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
            _routingConfiguration.Add<IIntegrationMessage>(TestEndpoint.Create("allMessages"));
            _routingConfiguration.Add<IIntegrationEvent>(TestEndpoint.Create("allEvents"));
            _routingConfiguration.Add<TestEventOne>(TestEndpoint.Create("eventOne"));
            _routingConfiguration.Add<TestEventTwo>(TestEndpoint.Create("eventTwo"));

            await _connector.OnMessageReceived(message);

            foreach (var expectedEndpointName in expectedEndpointNames)
            {
                Assert.That(_broker.SentMessages.Count(x => x.Endpoint.Name == expectedEndpointName), Is.EqualTo(1));
            }

            var notExpectedEndpointNames = _routingConfiguration
                .Routes.Select(r => r.DestinationEndpoint.Name)
                .Where(r => !expectedEndpointNames.Contains(r));

            foreach (var notExpectedEndpointName in notExpectedEndpointNames)
            {
                Assert.That(_broker.SentMessages.Count(x => x.Endpoint.Name == notExpectedEndpointName), Is.EqualTo(0));
            }
        }
    }
}