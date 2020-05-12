// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    public class OutboundQueueWorkerTests
    {
        private readonly InMemoryOutboundQueue _queue;
        private readonly TestBroker _broker;
        private readonly OutboundQueueWorker _worker;

        private readonly OutboundEnvelope _sampleOutboundEnvelope;

        public OutboundQueueWorkerTests()
        {
            _queue = new InMemoryOutboundQueue(new TransactionalListSharedItems<QueuedMessage>());

            var services = new ServiceCollection();

            services
                .AddSingleton<IOutboundQueueWriter>(_queue)
                .AddSingleton<IOutboundQueueReader>(_queue);

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>()
                    .AddDeferredOutboundConnector());

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>()
                .Add<IIntegrationMessage>(new StaticOutboundRouter(TestProducerEndpoint.GetDefault()));

            _broker = (TestBroker) serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();

            _worker = new OutboundQueueWorker(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                new BrokerCollection(new[] { _broker }),
                new NullLogger<OutboundQueueWorker>(),
                true,
                100); // TODO: Test order not enforced

            _sampleOutboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" }, null, TestProducerEndpoint.GetDefault());
            _sampleOutboundEnvelope.RawMessage =
                new JsonMessageSerializer().Serialize(_sampleOutboundEnvelope.Message, _sampleOutboundEnvelope.Headers,
                    MessageSerializationContext.Empty);
        }

        [Fact]
        public async Task ProcessQueue_SomeMessages_Produced()
        {
            await _queue.Enqueue(new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" }, null,
                new TestProducerEndpoint("topic1")));
            await _queue.Enqueue(new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" }, null,
                new TestProducerEndpoint("topic2")));
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedOnce()
        {
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);
            await _worker.ProcessQueue(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedNewMessages()
        {
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);

            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Commit();

            await _worker.ProcessQueue(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(3);
        }

        // TODO: Test retry and error handling?
    }
}