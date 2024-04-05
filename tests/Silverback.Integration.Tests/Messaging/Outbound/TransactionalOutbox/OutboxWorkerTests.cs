// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox
{
    public class OutboxWorkerTests
    {
        private readonly IOutboxWriter _outboxWriter;

        private readonly TestBroker _broker;

        private readonly IOutboxWorker _worker;

        private readonly OutboundEnvelope _sampleOutboundEnvelope;

        public OutboxWorkerTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()
                            .AddOutbox<InMemoryOutbox>()
                            .AddOutboxWorker())
                    .AddEndpoints(
                        endpoints => endpoints
                            .AddOutbound<TestEventOne>(new TestProducerEndpoint("topic1"))
                            .AddOutbound<TestEventTwo>(new TestProducerEndpoint("topic2"))
                            .AddOutbound<TestEventThree>(new TestProducerEndpoint("topic3a"))
                            .AddOutbound<TestEventThree>(new TestProducerEndpoint("topic3b"))));

            _broker = serviceProvider.GetRequiredService<TestBroker>();
            AsyncHelper.RunSynchronously(() => _broker.ConnectAsync());

            _worker = serviceProvider.GetRequiredService<IOutboxWorker>();
            _outboxWriter = serviceProvider.GetRequiredService<IOutboxWriter>();

            _sampleOutboundEnvelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" },
                null,
                new TestProducerEndpoint("topic1"));
            _sampleOutboundEnvelope.RawMessage =
                AsyncHelper.RunValueTaskSynchronously(
                    () => new JsonMessageSerializer().SerializeAsync(
                        _sampleOutboundEnvelope.Message,
                        _sampleOutboundEnvelope.Headers,
                        MessageSerializationContext.Empty));
        }

        [Fact]
        public async Task ProcessQueue_SomeMessages_Produced()
        {
            await _outboxWriter.WriteAsync(
                new TestEventOne { Content = "Test" },
                null,
                null,
                "topic1",
                "topic1");
            await _outboxWriter.WriteAsync(
                new TestEventTwo { Content = "Test" },
                null,
                null,
                "topic2",
                "topic2");
            await _outboxWriter.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
        }

        [Fact]
        public async Task ProcessQueue_SomeMessagesWithMultipleEndpoints_CorrectlyProduced()
        {
            await _outboxWriter.WriteAsync(
                new TestEventThree { Content = "Test" },
                null,
                null,
                "topic3a",
                "topic3a");
            await _outboxWriter.WriteAsync(
                new TestEventThree { Content = "Test" },
                null,
                null,
                "topic3b",
                "topic3b");
            await _outboxWriter.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic3a");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic3b");
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedOnce()
        {
            await _outboxWriter.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Name,
                _sampleOutboundEnvelope.ActualEndpointName);
            await _outboxWriter.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Name,
                _sampleOutboundEnvelope.ActualEndpointName);
            await _outboxWriter.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);
            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task ProcessQueue_RunTwice_ProducedNewMessages()
        {
            await _outboxWriter.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Name,
                _sampleOutboundEnvelope.ActualEndpointName);
            await _outboxWriter.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Name,
                _sampleOutboundEnvelope.ActualEndpointName);
            await _outboxWriter.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            await _outboxWriter.WriteAsync(
                _sampleOutboundEnvelope.Message,
                _sampleOutboundEnvelope.RawMessage.ReadAll(),
                _sampleOutboundEnvelope.Headers,
                _sampleOutboundEnvelope.Endpoint.Name,
                _sampleOutboundEnvelope.ActualEndpointName);
            await _outboxWriter.CommitAsync();

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(3);
        }

        [Fact]
        public async Task ProcessQueue_ProduceError_Retried()
        {
            await _outboxWriter.WriteAsync(
                new TestEventOne { Content = "Test" },
                null,
                null,
                "topic1",
                "topic1");
            await _outboxWriter.WriteAsync(
                new TestEventTwo { Content = "Test" },
                null,
                null,
                "topic2",
                "topic2");
            await _outboxWriter.WriteAsync(
                new TestEventOne { Content = "Test" },
                null,
                null,
                "topic1",
                "topic1");
            await _outboxWriter.WriteAsync(
                new TestEventOne { Content = "Test" },
                null,
                null,
                "topic1",
                "topic1");
            await _outboxWriter.CommitAsync();

            _broker.FailProduceNumber = new[] { 2, 3 }; // Note: counter is per producer / topic

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(2);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");

            await _worker.ProcessQueueAsync(CancellationToken.None);

            _broker.ProducedMessages.Should().HaveCount(4);
            _broker.ProducedMessages[0].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[1].Endpoint.Name.Should().Be("topic2");
            _broker.ProducedMessages[2].Endpoint.Name.Should().Be("topic1");
            _broker.ProducedMessages[3].Endpoint.Name.Should().Be("topic1");
        }
    }
}
