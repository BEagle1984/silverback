using System;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class SimpleInboudAdapterTests
    {
        [SetUp]
        public void Setup()
        {
            BrokersConfig.Instance.Clear();
            BrokersConfig.Instance.Add<TestBroker>(c => c.UseServer("server"));
        }

        [Test]
        public void RelayMessageTest()
        {
            using (var bus = new Bus())
            {
                int count = 0;
                bus.Config().Subscribe<IMessage>(m => count++);

                var adapter = new SimpleInboundAdapter();
                adapter.Init(bus, BasicEndpoint.Create("fake"));

                var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
                var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

                var consumer = (TestConsumer)BrokersConfig.Instance.Default.GetConsumer(BasicEndpoint.Create("test"));
                consumer.TestPush(e1);
                consumer.TestPush(e2);

                Assert.That(count, Is.EqualTo(2));
            }
        }
    }
}