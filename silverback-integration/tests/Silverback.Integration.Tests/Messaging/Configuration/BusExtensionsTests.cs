using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Integration;
using Silverback.Messaging.Integration.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Configuration
{
    [TestFixture]
    public class BusExtensionsTests
    {
        private InMemoryOutboundQueue _outboundQueue;
        private InMemoryInboundLog _inboundLog;

        [SetUp]
        public void Setup()
        {
            _outboundQueue = new InMemoryOutboundQueue();
            _outboundQueue.Clear();

            _inboundLog = new InMemoryInboundLog();
            _inboundLog.Clear();
        }

        [Test]
        public void AddOutboundTest()
        {
            using (var bus = new BusBuilder().WithFactory(t => Activator.CreateInstance(t, _outboundQueue)).Build())
            {
                bus.AddOutbound<TestEventOne, DeferredOutboundConnector>(BasicEndpoint.Create("topicEventOne"))
                    .AddOutbound<TestEventTwo, DeferredOutboundConnector>(BasicEndpoint.Create("topicEventTwo"))
                    .Subscribe<DeferredOutboundConnector>();

                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventTwo());

                bus.Publish(new TransactionCommitEvent());

                Assert.That(_outboundQueue.Length, Is.EqualTo(5));
            }
        }

        [Test]
        public void AddDeferredOutboundTest()
        {
            using (var bus = new BusBuilder().WithFactory(t => Activator.CreateInstance(t, _outboundQueue)).Build())
            {
                bus.AddDeferredOutbound<TestEventOne>(BasicEndpoint.Create("topicEventOne"))
                    .AddDeferredOutbound<TestEventTwo>(BasicEndpoint.Create("topicEventTwo"))
                    .Subscribe<DeferredOutboundConnector>();

                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventTwo());

                bus.Publish(new TransactionCommitEvent());

                Assert.That(_outboundQueue.Length, Is.EqualTo(5));
            }
        }

        [Test]
        public void AddDeferredOutboundRollbackTest()
        {
            using (var bus = new BusBuilder().WithFactory(t => Activator.CreateInstance(t, _outboundQueue)).Build())
            {
                bus.AddDeferredOutbound<TestEventOne>(BasicEndpoint.Create("topicEventOne"))
                    .AddDeferredOutbound<TestEventTwo>(BasicEndpoint.Create("topicEventTwo"))
                    .Subscribe<DeferredOutboundConnector>();

                bus.Publish(new TestEventOne());
                bus.Publish(new TestEventTwo());

                bus.Publish(new TransactionCommitEvent());

                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventTwo());

                bus.Publish(new TransactionRollbackEvent());

                Assert.That(_outboundQueue.Length, Is.EqualTo(2));
            }
        }
        [Test]
        public void AddDeferredOutboundWithFilterTest()
        {
            using (var bus = new BusBuilder().WithFactory(t => Activator.CreateInstance(t, _outboundQueue)).Build())
            {
                bus.AddDeferredOutbound<TestEventOne>(BasicEndpoint.Create("topicEventOne"), e => e.Content == "YES")
                    .AddDeferredOutbound<TestEventTwo>(BasicEndpoint.Create("topicEventTwo"))
                    .Subscribe<DeferredOutboundConnector>();

                bus.Publish(new TestEventOne {Content = "YES"});
                bus.Publish(new TestEventTwo());
                bus.Publish(new TestEventOne { Content = "YES" });
                bus.Publish(new TestEventOne { Content = "NO" });
                bus.Publish(new TestEventOne { Content = "NO" });

                bus.Publish(new TransactionCommitEvent());

                Assert.That(_outboundQueue.Length, Is.EqualTo(3));
            }
        }

        [Test]
        public void AddInboundTest()
        {
            using (var bus = new BusBuilder().Build())
            {
                var connector = new LoggedInboundConnector(_inboundLog);
                bus
                    .ConfigureBroker<TestBroker>()
                    .AddInbound(connector, BasicEndpoint.Create("test"))
                    .ConnectBrokers();

                var consumer = (TestConsumer)bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

                Assert.That(_inboundLog.Length, Is.EqualTo(5));
            }
        }

        [Test]
        public void AddLoggedInboundTest()
        {
            using (var bus = new BusBuilder().WithFactory(t => Activator.CreateInstance(t, _inboundLog)).Build())
            {
                bus.ConfigureBroker<TestBroker>()
                    .AddLoggedInbound(BasicEndpoint.Create("test"))
                    .ConnectBrokers();

                var consumer = (TestConsumer)bus.GetBroker().GetConsumer(BasicEndpoint.Create("test"));
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
                consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

                Assert.That(_inboundLog.Length, Is.EqualTo(5));
            }
        }

        [Test]
        public void AddTranslatorTest()
        {
            using (var bus = new BusBuilder().WithFactory(t => Activator.CreateInstance(t, _outboundQueue)).Build())
            {
                var outputMessages = new List<TestEventOne>();
                bus
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
            using (var bus = new BusBuilder().Build())
            {
                bus
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
            using (var bus = new BusBuilder().Build())
            {
                bus
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
            using (var bus = new BusBuilder().Build())
            {
                bus
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
