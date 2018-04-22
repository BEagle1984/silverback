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
                    .ConfigureBroker<TestBroker>(x => { })
                    .WithFactory(t => Activator.CreateInstance(t, _outboxRepository))
                    .AddInbound(adapter, BasicEndpoint.Create("test"))
                    .ConnectBrokers();

                var consumer = (TestConsumer)bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
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
                var outputMessages = new List<TestEventOne>();
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

        [Test]
        public void ConfigureBrokerTest()
        {
            using (var bus = new Bus())
            {
                bus.Config()
                    .ConfigureBroker<TestBroker>(b => b.WithName("Test1"))
                    .ConfigureBroker<TestBroker>(b => b.WithName("Test2"));

                var brokers = bus.GetBrokers();
                Assert.That(brokers.Count, Is.EqualTo(2));
                Assert.That(brokers.First(), Is.InstanceOf<TestBroker>());
                Assert.That(brokers.OfType<TestBroker>().First().Name, Is.EqualTo("Test1"));
            }
        }

        [Test]
        public void ConnectBrokersTest()
        {
            using (var bus = new Bus())
            {
                bus.Config()
                    .ConfigureBroker<TestBroker>(b => b.WithName("Test1"))
                    .ConfigureBroker<TestBroker>(b => b.WithName("Test2"));

                bus.ConnectBrokers();

                var brokers = bus.GetBrokers();
                Assert.That(brokers.All(b => b.IsConnected));
            }
        }

        [Test]
        public void DisconnectBrokersTest()
        {
            using (var bus = new Bus())
            {
                bus.Config()
                    .ConfigureBroker<TestBroker>(b => b.WithName("Test1"))
                    .ConfigureBroker<TestBroker>(b => b.WithName("Test2"));

                bus.ConnectBrokers();
                bus.DisconnectBrokers();

                var brokers = bus.GetBrokers();
                Assert.That(brokers.All(b => !b.IsConnected));
            }
        }
    }
}
