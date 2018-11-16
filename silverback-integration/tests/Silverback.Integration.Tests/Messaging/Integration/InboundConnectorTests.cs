using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using System;
using Silverback.Messaging.Broker;

namespace Silverback.Tests.Messaging.Integration
{
    [TestFixture]
    public class InboundConnectorTests
    {
        private IServiceCollection _services;
        private TestSubscriber _testSubscriber;
        private IInboundConnector _connector;
        private TestBroker _broker;

        [SetUp]
        public void Setup()
        {
            _services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();
            _services.AddSingleton<ISubscriber>(_testSubscriber);

            _services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            _services.AddSingleton<IPublisher, Publisher>();

            _broker = new TestBroker(new JsonMessageSerializer());
            _services.AddSingleton<IBroker>(_broker);

            _connector = new InboundConnector(_broker, _services.BuildServiceProvider(), NullLoggerFactory.Instance);
        }

        [Test]
        public void Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(5));
        }

        [Test]
        public void Bind_WithRetryErrorPolicy_RetriedAndReceived()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, policy => policy.Retry(3));
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.FailCount, Is.EqualTo(3));
            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void Bind_WithChainedErrorPolicy_RetriedAndMoved()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, policy => policy.Chain(
                p => p.Retry(1),
                p => p.Move(TestEndpoint.Create("bad"))));
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            var producer = (TestProducer)_broker.GetProducer(TestEndpoint.Create("bad"));

            Assert.That(_testSubscriber.FailCount, Is.EqualTo(2));
            Assert.That(producer.SentMessages.Count, Is.EqualTo(1));
            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(0));
        }
    }
}