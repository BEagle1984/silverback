using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Configuration
{
    [TestFixture]
    public class DependencyInjectionExtensionsTests
    {
        private IServiceCollection _services;
        private TestSubscriber _testSubscriber;
        private IServiceProvider _serviceProvider;

        private IServiceProvider GetServiceProvider() => _serviceProvider ?? (_serviceProvider = _services.BuildServiceProvider());

        private TestBroker GetBroker() => (TestBroker) GetServiceProvider().GetService<IBroker>();
        private IPublisher GetPublisher() => GetServiceProvider().GetService<IPublisher>();

        private IOutboundRoutingConfiguration GetOutboundRouting() => GetServiceProvider().GetService<IOutboundRoutingConfiguration>();
        private InMemoryOutboundQueue GetOutboundQueue() => (InMemoryOutboundQueue)GetServiceProvider().GetService<IOutboundQueueWriter>();

        private IInboundConnector GetInboundConnector() => GetServiceProvider().GetService<IInboundConnector>();
        private InMemoryInboundLog GetInboundLog() => (InMemoryInboundLog)GetServiceProvider().GetService<IInboundLog>();

        [SetUp]
        public void Setup()
        {
            _services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();
            _services.AddSingleton<ISubscriber>(_testSubscriber);

            _services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            _services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            _services.AddSingleton<IPublisher, Publisher>();

            _serviceProvider = null; // Creation deferred to after AddBroker() has been called
        }

        [Test]
        public void AddBrokerTest()
        {
            _services.AddBroker<TestBroker>(options => { });

            Assert.That(GetBroker(), Is.Not.Null);
        }

        [Test]
        public void DefaultSerializerTest()
        {
            _services.AddBroker<TestBroker>(options => { });

            Assert.That(GetBroker().Serializer, Is.InstanceOf<JsonMessageSerializer>());
        }

        [Test]
        public void UseSerializerTest()
        {
            _services.AddBroker<TestBroker>(options => options.UseSerializer<FakeSerializer>());

            Assert.That(GetBroker().Serializer, Is.InstanceOf<FakeSerializer>());
        }

        [Test]
        public void AddOutboundTest()
        {
            _services.AddBroker<TestBroker>(options => options.AddOutboundConnector());
            GetOutboundRouting().Add<IIntegrationMessage>(TestEndpoint.Default);

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());

            Assert.That(GetBroker().SentMessages.Count, Is.EqualTo(5));
        }

        [Test]
        public void AddDeferredOutboundTest()
        {
            _services.AddBroker<TestBroker>(options => options.AddDeferredOutboundConnector<InMemoryOutboundQueue>());
            GetOutboundRouting().Add<IIntegrationMessage>(TestEndpoint.Default);

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionCommitEvent());

            Assert.That(GetOutboundQueue().Length, Is.EqualTo(5));
        }

        [Test]
        public void AddDeferredOutboundRollbackTest()
        {
            _services.AddBroker<TestBroker>(options => options.AddDeferredOutboundConnector<InMemoryOutboundQueue>());
            GetOutboundRouting().Add<IIntegrationMessage>(TestEndpoint.Default);

            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionCommitEvent());
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TransactionRollbackEvent());

            Assert.That(GetOutboundQueue().Length, Is.EqualTo(2));
        }

        [Test]
        public void AddOutboundRoutingTest()
        {
            _services.AddBroker<TestBroker>(options => options.AddOutboundConnector());

            GetOutboundRouting()
                .Add<TestEventOne>(TestEndpoint.Create("test1"))
                .Add<IIntegrationEvent>(TestEndpoint.Create("test2"));

            // -> to both endpoints
            GetPublisher().Publish(new TestEventOne());
            GetPublisher().Publish(new TestEventOne());
            // -> to test2
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            GetPublisher().Publish(new TestEventTwo());
            // -> to nowhere
            GetPublisher().Publish(new TestInternalEventOne());
            
            Assert.That(GetBroker().SentMessages.Count, Is.EqualTo(7));
            Assert.That(GetBroker().SentMessages.Where(x => x.Endpoint.Name == "test1").Count, Is.EqualTo(2));
            Assert.That(GetBroker().SentMessages.Where(x => x.Endpoint.Name == "test2").Count, Is.EqualTo(5));
        }

        [Test]
        public void AddInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddBroker<TestBroker>(options => options.AddInboundConnector());

            GetInboundConnector().Bind(TestEndpoint.Default);
            GetBroker().Connect();

            var consumer = (TestConsumer)GetBroker().GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(5));
        }

        [Test]
        public void AddLoggedInboundConnector_PushMessages_MessagesReceived()
        {
            _services.AddBroker<TestBroker>(options => options.AddLoggedInboundConnector<InMemoryInboundLog>());

            GetInboundConnector().Bind(TestEndpoint.Default);
            GetBroker().Connect();

            var consumer = (TestConsumer)GetBroker().GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(GetInboundLog().Length, Is.EqualTo(5));
        }
    }
}
