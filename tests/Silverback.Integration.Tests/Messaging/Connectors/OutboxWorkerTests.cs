// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Connectors.Repositories.Model;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors
{
    public class OutboxWorkerTests
    {
        private readonly InMemoryOutbox _queue;

        private readonly TestBroker _broker;

        private readonly OutboxWorker _worker;

        private readonly OutboundEnvelope _sampleOutboundEnvelope;

        public OutboxWorkerTests()
        {
            _queue = new InMemoryOutbox(new TransactionalListSharedItems<OutboxStoredMessage>());

            var services = new ServiceCollection();

            services
                .AddSingleton<IOutboxWriter>(_queue)
                .AddSingleton<IOutboxReader>(_queue);

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddOutbox<InMemoryOutbox>());

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            var routingConfiguration = serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
            routingConfiguration.Add<TestEventOne>(_ => new StaticOutboundRouter(new TestProducerEndpoint("topic1")));
            routingConfiguration.Add<TestEventTwo>(_ => new StaticOutboundRouter(new TestProducerEndpoint("topic2")));
            routingConfiguration.Add<TestEventThree>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("topic3a")));
            routingConfiguration.Add<TestEventThree>(
                _ => new StaticOutboundRouter(new TestProducerEndpoint("topic3b")));

            _broker = (TestBroker)serviceProvider.GetRequiredService<IBroker>();
            _broker.Connect();

            _worker = new OutboxWorker(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                new BrokerCollection(new[] { _broker }),
                routingConfiguration,
                Substitute.For<ISilverbackIntegrationLogger<OutboxWorker>>(),
                true,
                100); // TODO: Test order not enforced

            _sampleOutboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" },
                null,
                new TestProducerEndpoint("topic1"));
            _sampleOutboundEnvelope.RawMessage =
                AsyncHelper.RunSynchronously(
                    () => new JsonMessageSerializer().SerializeAsync(
                        _sampleOutboundEnvelope.Message,
                        _sampleOutboundEnvelope.Headers,
                        MessageSerializationContext.Empty));
        }

        [Fact]
        public async Task ProcessQueue_SomeMessages_Produced()
        {
            await _queue.WriteAsync(
                new OutboundEnvelope<TestEventOne>(
                    new TestEventOne { Content = "Test" },
                    null,
                    new TestProducerEndpoint("topic1")));
            await _queue.WriteAsync(
                new OutboundEnvelope<TestEventTwo>(
                    new TestEventTwo { Content = "Test" },
                    null,
                    new TestProducerEndpoint("topic2")));
            await _queue.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
        }

        [Fact]
        public async Task ProcessQueue_SomeMessagesWithMultipleEndpoints_CorrectlyProduced()
        {
            await _queue.WriteAsync(
                new OutboundEnvelope<TestEventThree>(
                    new TestEventThree { Content = "Test" },
                    null,
                    new TestProducerEndpoint("topic3a")));
            await _queue.WriteAsync(
                new OutboundEnvelope<TestEventThree>(
                    new TestEventThree { Content = "Test" },
                    null,
                    new TestProducerEndpoint("topic3b")));
            await _queue.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic3a");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic3b");
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedOnce()
        {
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);
            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(2);
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedNewMessages()
        {
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Count.Should().Be(3);
        }

        // TODO: Test retry and error handling?
    }
}
