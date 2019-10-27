// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
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

        private readonly OutboundMessage _sampleOutboundMessage;

        public OutboundQueueWorkerTests()
        {
            _queue = new InMemoryOutboundQueue();

            var services = new ServiceCollection();

            services
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddSingleton<IOutboundQueueConsumer, InMemoryOutboundQueue>()
                .AddSilverback().WithConnectionTo<TestBroker>(options => options.AddDeferredOutboundConnector(_ => new InMemoryOutboundQueue()));

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>()
                .Add<IIntegrationMessage>(TestEndpoint.Default);

            _broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();

            _worker = new OutboundQueueWorker(serviceProvider, _broker, new NullLogger<OutboundQueueWorker>(), new MessageLogger(), true, 100); // TODO: Test order not enforced

            InMemoryOutboundQueue.Clear();

            _sampleOutboundMessage = new OutboundMessage<TestEventOne>(
                new TestEventOne { Content = "Test" }, null, TestEndpoint.Default);
            _sampleOutboundMessage.RawContent =
                new JsonMessageSerializer().Serialize(_sampleOutboundMessage.Content, _sampleOutboundMessage.Headers);
        }

        [Fact]
        public async Task ProcessQueue_SomeMessages_Produced()
        {
            await _queue.Enqueue(new OutboundMessage<TestEventOne>(
                new TestEventOne { Content = "Test" }, null,
                new TestEndpoint("topic1")));
            await _queue.Enqueue(new OutboundMessage<TestEventOne>(
                new TestEventOne { Content = "Test" }, null,
                new TestEndpoint("topic2")));
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
        }
        
        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedOnce()
        {
            await _queue.Enqueue(_sampleOutboundMessage);
            await _queue.Enqueue(_sampleOutboundMessage);
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);
            await _worker.ProcessQueue(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedNewMessages()
        {
            await _queue.Enqueue(_sampleOutboundMessage);
            await _queue.Enqueue(_sampleOutboundMessage);
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);

            await _queue.Enqueue(_sampleOutboundMessage);
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(3);
        }

        // TODO: Test retry and error handling?
    }
}