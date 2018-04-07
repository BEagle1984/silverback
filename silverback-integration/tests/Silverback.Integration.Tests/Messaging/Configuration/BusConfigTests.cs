using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Adapters;
using Silverback.Messaging.Configuration;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Configuration
{
    [TestFixture]
    public class BusConfigTests
    {
        private OutboundMessagesRepository _outboxRepository;
        private InboundMessagesRepository _inboxRepository;

        [SetUp]
        public void Setup()
        {
            BrokersConfig.Instance.Clear();
            BrokersConfig.Instance.Add<TestBroker>(c => c.UseServer("server"));

            _outboxRepository = new OutboundMessagesRepository();
            _inboxRepository = new InboundMessagesRepository();
        }

        [Test]
        public void AddOutboundTest()
        {
            using (var bus = new Bus())
            {
                bus.Config()
                    .WithFactory(t => Activator.CreateInstance(t, _outboxRepository))
                    .AddOutbound<TestEventOne, DbOutboundAdapter<OutboundMessageEntity>>(BasicEndpoint.Create("topicEventOne"))
                    .AddOutbound<TestEventTwo, DbOutboundAdapter<OutboundMessageEntity>>(BasicEndpoint.Create("topicEventTwo"));

                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventTwo());

                Assert.That(_outboxRepository.DbSet.Count, Is.EqualTo(5));
                Assert.That(_outboxRepository.DbSet.Count(m => m.MessageType == typeof(TestEventOne).AssemblyQualifiedName), Is.EqualTo(2));
                Assert.That(_outboxRepository.DbSet.Count(m => m.MessageType == typeof(TestEventTwo).AssemblyQualifiedName), Is.EqualTo(3));
            }
        }

        [Test]
        public void AddInboundTest()
        {
            using (var bus = new Bus())
            {
                var adapter = new DbInboundAdapter<InboundMessageEntity>(_inboxRepository);
                bus.Config()
                    .WithFactory(t => Activator.CreateInstance(t, _outboxRepository))
                    .AddInbound(adapter, BasicEndpoint.Create("fake"));

                var consumer = (TestConsumer)BrokersConfig.Instance.Default.GetConsumer(BasicEndpoint.Create("test"));
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

                Assert.That(_inboxRepository.DbSet.Count, Is.EqualTo(5));
            }
        }

        [Test]
        public void AddTranslatorTest()
        {
            using (var bus = new Bus())
            {
                List<TestEventOne> outputMessages = new List<TestEventOne>();
                bus.Config()
                    .WithFactory(t => Activator.CreateInstance(t, _outboxRepository))
                    .AddTranslator<TestInternalEventOne, TestEventOne>(m => new TestEventOne { Content = m.InternalMessage })
                    .Subscribe<TestEventOne>(m => outputMessages.Add(m));

                bus.Publish(new TestInternalEventOne());
                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestInternalEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventTwo());

                Assert.That(outputMessages.Count, Is.EqualTo(3));
            }
        }
    }
}
