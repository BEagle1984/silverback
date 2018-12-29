// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
{
    [TestFixture]
    public class OutboundQueueWorkerTests
    {
        private InMemoryOutboundQueue _queue;
        private TestBroker _broker;
        private OutboundQueueWorker _worker;

        [SetUp]
        public void Setup()
        {
            _queue = new InMemoryOutboundQueue();

            var services = new ServiceCollection();

            services
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddBus()
                .AddBroker<TestBroker>(options => options.AddDeferredOutboundConnector<InMemoryOutboundQueue>());

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>()
                .Add<IIntegrationMessage>(TestEndpoint.Default);

            _broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();

            _worker = new OutboundQueueWorker(_queue, _broker, new NullLogger<OutboundQueueWorker>(), true, 100); // TODO: Test order not enforced

            InMemoryOutboundQueue.Clear();
        }

        [Test]
        public void ProcessQueue_SomeMessages_Produced()
        {
            _queue.Enqueue(new TestEventOne { Content = "1" }, new TestEndpoint("topic1"));
            _queue.Enqueue(new TestEventTwo { Content = "2" }, new TestEndpoint("topic2"));
            _queue.Commit();

            _worker.ProcessQueue();

            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(2));
            Assert.That(_broker.ProducedMessages[0].Endpoint.Name, Is.EqualTo("topic1"));
            Assert.That(_broker.ProducedMessages[1].Endpoint.Name, Is.EqualTo("topic2"));
        }
        
        [Test]
        public void ProcessQueue_RunTwice_ProducedOnce()
        {
            _queue.Enqueue(new TestEventOne { Content = "1" }, new TestEndpoint("topic1"));
            _queue.Enqueue(new TestEventTwo { Content = "2" }, new TestEndpoint("topic2"));
            _queue.Commit();

            _worker.ProcessQueue();
            _worker.ProcessQueue();

            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProcessQueue_RunTwice_ProducedNewMessages()
        {
            _queue.Enqueue(new TestEventOne { Content = "1" }, new TestEndpoint("topic1"));
            _queue.Enqueue(new TestEventTwo { Content = "2" }, new TestEndpoint("topic2"));
            _queue.Commit();

            _worker.ProcessQueue();

            _queue.Enqueue(new TestEventTwo { Content = "3" }, new TestEndpoint("topic3"));
            _queue.Commit();

            _worker.ProcessQueue();

            Assert.That(_broker.ProducedMessages.Count, Is.EqualTo(3));
        }

        // TODO: Test retry and error handling?
    }
}