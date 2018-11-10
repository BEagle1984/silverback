using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Integration;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Integration
{
    [TestFixture]
    public class OutboudConnectorTests
    {
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _broker = new TestBroker().UseServer("server");
        }

        [Test]
        public void RelayTest()
        {
            var connector = new OutboundConnector();

            var message = new TestEventOne {Content = "Test"};
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");
            connector.Relay(message, _broker.GetProducer(endpoint), endpoint);

            var producer = (TestProducer)_broker.GetProducer(BasicEndpoint.Create("test"));
            var serializer = producer.Serializer;

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
            Assert.That(serializer.Deserialize(producer.SentMessages.First()).Message.Id, Is.EqualTo(message.Id));
        }

        [Test]
        public async Task RelayAsyncTest()
        {
            var connector = new OutboundConnector();

            var message = new TestEventOne { Content = "Test" };
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");
            await connector.RelayAsync(message, _broker.GetProducer(endpoint), endpoint);

            var producer = (TestProducer)_broker.GetProducer(BasicEndpoint.Create("test"));
            var serializer = producer.Serializer;

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
            Assert.That(serializer.Deserialize(producer.SentMessages.First()).Message.Id, Is.EqualTo(message.Id));
        }
    }
}