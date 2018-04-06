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

        [SetUp]
        public void Setup()
        {
            BrokersConfig.Instance.Clear();
            BrokersConfig.Instance.Add<FakeBroker>(c => c.UseServer("server"));

            _repository = new InboundMessagesRepository();
        }

        [Test]
        public void RelayMessageTest()
        {
            using (var bus = new Bus())
            {
                int count = 0;
                bus.Config().Subscribe<IMessage>(m => count++);

                var adapter = new DbInboundAdapter<InboundMessageEntity>(_repository);
                adapter.Init(bus, BasicEndpoint.Create("fake"));

                var e1 = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };
                var e2 = new TestEventTwo { Content = "Test", Id = Guid.NewGuid() };

                var consumer = (FakeConsumer)BrokersConfig.Instance.Default.GetConsumer(BasicEndpoint.Create("test"));
                consumer.TestPush(e1);
                consumer.TestPush(e2);

                Assert.That(count, Is.EqualTo(2));
            }
        }

        [Test]
        public void AddToInboxTest()
        {
            var bus = new Bus();
            var adapter = new DbInboundAdapter<InboundMessageEntity>(_repository);
            adapter.Init(bus, BasicEndpoint.Create("fake"));

            var e = new TestEventOne { Content = "Test", Id = Guid.NewGuid() };

            var consumer = (FakeConsumer)BrokersConfig.Instance.Default.GetConsumer(BasicEndpoint.Create("test"));
            consumer.TestPush(e);

            Assert.That(_repository.DbSet.Count, Is.EqualTo(1));
            var entity = _repository.DbSet.First();
            Assert.That(entity.MessageId, Is.EqualTo(e.Id));
        }
    }
}