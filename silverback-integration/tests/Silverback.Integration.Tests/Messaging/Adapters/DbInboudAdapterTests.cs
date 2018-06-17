using System;
using System.Linq;
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
    public class DbInboudAdapterTests
    {
        private InboundMessagesRepository _repository;
        private Bus _bus;

        [SetUp]
        public void Setup()
        {
            _bus = BusConfig.Create<Bus>(c => c
                .ConfigureBroker<TestBroker>(x => x
                    .UseServer("server")
                )
            );

            _repository = new InboundMessagesRepository();
        }

        [Test]
        public void RelayMessageTest()
        {
            int count = 0;
            _bus.Config().Subscribe<IMessage>(m => count++);

            var adapter = new DbInboundAdapter<InboundMessageEntity>(_repository);
            adapter.Init(_bus, BasicEndpoint.Create("test"));

            _bus.ConnectBrokers();

            var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
            var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

            var consumer = (TestConsumer)_bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
            consumer.TestPush(e1);
            consumer.TestPush(e2);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void AddToInboxTest()
        {
            var adapter = new DbInboundAdapter<InboundMessageEntity>(_repository);
            adapter.Init(_bus, BasicEndpoint.Create("test"));

            _bus.ConnectBrokers();

            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };

            var consumer = (TestConsumer)_bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
            consumer.TestPush(e);

            Assert.That(_repository.DbSet.Count, Is.EqualTo(1));
            var entity = _repository.DbSet.First();
            Assert.That(entity.MessageId, Is.EqualTo(e.Id));
        }
    }
}