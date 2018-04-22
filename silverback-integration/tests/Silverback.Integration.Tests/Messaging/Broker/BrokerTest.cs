using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes;

namespace Silverback.Tests.Messaging.Broker
{
    [TestFixture]
    public class BrokerTest
    {
        [Test]
        public void GetDefaultSerializerTest()
        {
            var serializer = new TestBroker().GetSerializer();

            Assert.That(serializer, Is.InstanceOf<JsonMessageSerializer>());
        }

        [Test]
        public void GetSerializerTest()
        {
            var broker = new TestBroker();
            broker.SerializeUsing<FakeSerializer>();
            var serializer = broker.GetSerializer();

            Assert.That(serializer, Is.InstanceOf<FakeSerializer>());
        }

        [Test]
        public void GetCachedSerializerTest()
        {
            var broker = new TestBroker();
            broker.SerializeUsing<FakeSerializer>();
            var serializer = broker.GetSerializer();
            var serializer2 = broker.GetSerializer();
            Assert.That(serializer, Is.SameAs(serializer2));
        }

        [Test]
        public void WithNameTest()
        {
            var broker = new TestBroker().WithName("TestBroker");

            Assert.That(broker.Name, Is.EqualTo("TestBroker"));
        }

        [Test]
        public void AsDefaultTest()
        {
            var broker = new TestBroker();
            Assert.That(broker.IsDefault, Is.False);

            broker.AsDefault();

            Assert.That(broker.IsDefault, Is.True);
        }

        [Test]
        public void GetProducerTest()
        {
            var broker = new TestBroker();
            var producer = broker.GetProducer(BasicEndpoint.Create("test"));

            Assert.That(producer, Is.Not.Null);
        }

        [Test]
        public void GetCachedProducerTest()
        {
            var broker = new TestBroker();
            var producer = broker.GetProducer(BasicEndpoint.Create("test"));
            var producer2 = broker.GetProducer(BasicEndpoint.Create("test"));

            Assert.That(producer2, Is.SameAs(producer));
        }

        [Test]
        public void GetConsumerTest()
        {
            var broker = new TestBroker();
            var producer = broker.GetConsumer(BasicEndpoint.Create("test"));

            Assert.That(producer, Is.Not.Null);
        }

        [Test]
        public void GetCachedConsumerTest()
        {
            var broker = new TestBroker();
            var producer = broker.GetConsumer(BasicEndpoint.Create("test"));
            var producer2 = broker.GetConsumer(BasicEndpoint.Create("test"));

            Assert.That(producer2, Is.SameAs(producer));
        }
    }
}
