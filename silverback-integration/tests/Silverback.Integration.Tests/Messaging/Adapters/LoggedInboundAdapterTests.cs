using System;
using System.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Repositories;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Adapters
{
    [TestFixture]
    public class LoggedInboundAdapterTests
    {
        private IInboundLog _inboundLog;
        private IBus _bus;
        private LoggedInboundAdapter _adapter;
        private TestConsumer _testConsumer;

        [SetUp]
        public void Setup()
        {
            _bus = new BusBuilder().Build()
                .ConfigureBroker<TestBroker>(x => x
                    .UseServer("server")
                );

            _inboundLog = new InMemoryInboundLog();

            _adapter = new LoggedInboundAdapter(_inboundLog);

            _adapter.Init(_bus, BasicEndpoint.Create("test"));
            _bus.ConnectBrokers();

            _testConsumer = (TestConsumer)_bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
        }

        [Test]
        public void RelayMessageTest()
        {
            int count = 0;
            _bus.Subscribe<IMessage>(m => count++);

            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _testConsumer.TestPush(e1);
            _testConsumer.TestPush(e2);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void RelayMessageOnceTest()
        {
            int count = 0;
            _bus.Subscribe<IMessage>(m => count++);

            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _testConsumer.TestPush(e1);
            _testConsumer.TestPush(e2);
            _testConsumer.TestPush(e1);
            _testConsumer.TestPush(e2);
            _testConsumer.TestPush(e2);
            _testConsumer.TestPush(e1);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void AddToLogTest()
        {
            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            _testConsumer.TestPush(e1);
            _testConsumer.TestPush(e2);
            _testConsumer.TestPush(e1);
            _testConsumer.TestPush(e2);

            Assert.That(_inboundLog.Length, Is.EqualTo(2));
        }
    }
}