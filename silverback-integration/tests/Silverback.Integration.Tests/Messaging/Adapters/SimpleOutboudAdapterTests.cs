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
        [SetUp]
        public void Setup()
        {
            BrokersConfig.Instance.Clear();
            BrokersConfig.Instance.Add<TestBroker>(c => c.UseServer("server"));
        }

        [Test]
        public void RelayTest()
        {
            var adapter = new SimpleOutboundAdapter();

            var e = new TestEventOne {Content = "Test"};
            adapter.Relay(e, BasicEndpoint.Create("TestEventOneTopic"));

            var producer = (TestProducer)BrokersConfig.Instance.Default.GetProducer(BasicEndpoint.Create("test"));
            var serializer = BrokersConfig.Instance.GetDefault<TestBroker>().GetSerializer();

            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
            Assert.That(serializer.Deserialize(producer.SentMessages.First()).Message.Id, Is.EqualTo(e.Id));
        }
    }
}