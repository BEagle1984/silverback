using System;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class SimpleInboudAdapterTests
    {
        private Bus _bus;

        [SetUp]
        public void Setup()
        {
            _bus = BusConfig.Create<Bus>(c => c
                .ConfigureBroker<TestBroker>(x => x
                    .UseServer("server")
                )
            );
        }

        [Test]
        public void RelayMessageTest()
        {
            int count = 0;
            _bus.Config().Subscribe<IMessage>(m => count++);

            var adapter = new SimpleInboundAdapter();
            adapter.Init(_bus, _bus.GetBroker(), BasicEndpoint.Create("test"));

            _bus.ConnectBrokers();

            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            var consumer = (TestConsumer)_bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
            consumer.TestPush(e1);
            consumer.TestPush(e2);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void ErrorPolicyTest()
        {
            int count = 0;
            _bus.Config().Subscribe<IMessage>(m =>
            {
                count++;
                if (count < 4)
                    throw new Exception("Retry please");
            });

            var adapter = new SimpleInboundAdapter();
            adapter.Init(_bus, _bus.GetBroker(), BasicEndpoint.Create("test"), new RetryErrorPolicy(5));

            _bus.ConnectBrokers();

            var consumer = (TestConsumer)_bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            Assert.That(count, Is.EqualTo(4));
        }
    }
}