// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Connectors
{
    [Collection("StaticInMemory")]
    public class OutboundQueueWorkerTests
    {
        private readonly InMemoryOutboundQueue _queue;
        private readonly TestBroker _broker;
        private readonly OutboundQueueWorker _worker;

        public OutboundQueueWorkerTests()
        {
            _queue = new InMemoryOutboundQueue();

            var services = new ServiceCollection();

            services
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddBus()
                .AddBroker<TestBroker>(options => options.AddDeferredOutboundConnector(_ => new InMemoryOutboundQueue()));

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>()
                .Add<IIntegrationMessage>(TestEndpoint.Default);

            _broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();

            _worker = new OutboundQueueWorker(_queue, _broker, new NullLogger<OutboundQueueWorker>(), new MessageLogger(new MessageKeyProvider(new[] { new DefaultPropertiesMessageKeyProvider() })), true, 100); // TODO: Test order not enforced

            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public void ProcessQueue_SomeMessages_Produced()
        {
            _queue.Enqueue(new TestEventOne { Content = "1" }, new TestEndpoint("topic1"));
            _queue.Enqueue(new TestEventTwo { Content = "2" }, new TestEndpoint("topic2"));
            _queue.Commit();

            _worker.ProcessQueue();

            _broker.ProducedMessages.Count.Should().Be(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
        }
        
        [Fact]
        public void ProcessQueue_RunTwice_ProducedOnce()
        {
            _queue.Enqueue(new TestEventOne { Content = "1" }, new TestEndpoint("topic1"));
            _queue.Enqueue(new TestEventTwo { Content = "2" }, new TestEndpoint("topic2"));
            _queue.Commit();

            _worker.ProcessQueue();
            _worker.ProcessQueue();

            _broker.ProducedMessages.Count.Should().Be(2);
        }

        [Fact]
        public void ProcessQueue_RunTwice_ProducedNewMessages()
        {
            _queue.Enqueue(new TestEventOne { Content = "1" }, new TestEndpoint("topic1"));
            _queue.Enqueue(new TestEventTwo { Content = "2" }, new TestEndpoint("topic2"));
            _queue.Commit();

            _worker.ProcessQueue();

            _queue.Enqueue(new TestEventTwo { Content = "3" }, new TestEndpoint("topic3"));
            _queue.Commit();

            _worker.ProcessQueue();

            _broker.ProducedMessages.Count.Should().Be(3);
        }

        // TODO: Test retry and error handling?
    }
}