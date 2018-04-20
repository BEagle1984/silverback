using System.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class SimpleOutboudAdapterTests
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
            var adapter = new SimpleOutboundAdapter();

            var @event = new TestEventOne {Content = "Test"};
            var endpoint = BasicEndpoint.Create("TestEventOneTopic");
            adapter.Relay(@event, _broker.GetProducer(endpoint), endpoint);

            var producer = (TestProducer)_broker.GetProducer(BasicEndpoint.Create("test"));
            var serializer = producer.Serializer;

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
            Assert.That(serializer.Deserialize(producer.SentMessages.First()).Message.Id, Is.EqualTo(@event.Id));
        }
    }
}