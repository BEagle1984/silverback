// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    [Collection("StaticInMemory")]
    public class OutboundQueueWorkerTests
    {
        private readonly InMemoryOutboundQueue _queue;
        private readonly TestBroker _broker;
        private readonly OutboundQueueWorker _worker;

        private readonly IOutboundMessage _sampleOutboundMessage = new OutboundMessage<TestEventOne>
        {
            Message = new TestEventOne {Content = "Test"},
            Endpoint = TestEndpoint.Default
        };

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
            _queue.Enqueue(new OutboundMessage<TestEventOne>
            {
                Message = new TestEventOne { Content = "Test" },
                Endpoint = new TestEndpoint("topic1")
            });
            _queue.Enqueue(new OutboundMessage<TestEventOne>
            {
                Message = new TestEventOne { Content = "Test" },
                Endpoint = new TestEndpoint("topic2")
            });
            _queue.Commit();

            _worker.ProcessQueue();

            _broker.ProducedMessages.Count.Should().Be(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
        }
        
        [Fact]
        public void ProcessQueue_RunTwice_ProducedOnce()
        {
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Commit();

            _worker.ProcessQueue();
            _worker.ProcessQueue();

            _broker.ProducedMessages.Count.Should().Be(2);
        }

        [Fact]
        public void ProcessQueue_RunTwice_ProducedNewMessages()
        {
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Commit();

            _worker.ProcessQueue();

            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Commit();

            _worker.ProcessQueue();

            _broker.ProducedMessages.Count.Should().Be(3);
        }

        // TODO: Test retry and error handling?
    }
}