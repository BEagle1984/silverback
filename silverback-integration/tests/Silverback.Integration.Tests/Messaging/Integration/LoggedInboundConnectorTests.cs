using System;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Integration.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Integration
{
    [TestFixture]
    public class LoggedInboundConnectorTests
    {
        private IInboundLog _inboundLog;
        private IBus _bus;
        private LoggedInboundConnector _connector;
        private TestConsumer _testConsumer;

        [SetUp]
        public void Setup()
        {
            _bus = new BusBuilder().Build()
                .ConfigureBroker<TestBroker>(x => x
                    .UseServer("server")
                );

            _inboundLog = new InMemoryInboundLog();

            _connector = new LoggedInboundConnector(_inboundLog);

            _connector.Init(_bus, BasicEndpoint.Create("test"));
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