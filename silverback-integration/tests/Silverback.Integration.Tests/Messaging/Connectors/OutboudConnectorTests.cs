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
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker();
            _connector = new OutboundConnector(_broker);
        }

        [Test]
        public async Task OnMessageReceived_SingleMessage_Relayed()
        {
            var endpoint = TestEndpoint.Default;

            var message = new TestEventOne { Content = "Test" };

            await _connector.RelayMessage(message, endpoint);

            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(1));
            Assert.That(_broker.ProducedMessages.First().Endpoint, Is.EqualTo(endpoint));

            var producedMessage = endpoint.Serializer.Deserialize(_broker.ProducedMessages.First().Message) as TestEventOne;
            Assert.That(producedMessage.Id, Is.EqualTo(message.Id));
        }
    }
}