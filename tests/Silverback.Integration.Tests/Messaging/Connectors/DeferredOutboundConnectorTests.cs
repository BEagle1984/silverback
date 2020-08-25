// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
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
    public class DeferredOutboundConnectorTests
    {
        private readonly InMemoryOutboundQueue _queue;

        private readonly DeferredOutboundConnector _connector;

        private readonly DeferredOutboundConnectorTransactionManager _transactionManager;

        public DeferredOutboundConnectorTests()
        {
            var services = new ServiceCollection();
            services.AddNullLogger()
                .AddSingleton(typeof(ISilverbackIntegrationLogger<>), typeof(IntegrationLoggerSubstitute<>))
                .AddSilverback();
            var serviceProvider = services.BuildServiceProvider();

            _queue = new InMemoryOutboundQueue(new TransactionalListSharedItems<QueuedMessage>());
            var broker = new OutboundQueueBroker(_queue, Array.Empty<IBrokerBehavior>(), serviceProvider);
            _connector = new DeferredOutboundConnector(
                broker,
                Substitute.For<ISilverbackIntegrationLogger<DeferredOutboundConnector>>());
            _transactionManager = new DeferredOutboundConnectorTransactionManager(_queue);
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Queued()
        {
            var message = new TestEventOne { Content = "Test" };
            var headers = new[]
            {
                new MessageHeader("header1", "value1"),
                new MessageHeader("header2", "value2")
            };
            var envelope = new OutboundEnvelope<TestEventOne>(
                message,
                headers,
                TestProducerEndpoint.GetDefault());
            envelope.RawMessage =
                new JsonMessageSerializer().Serialize(
                    envelope.Message,
                    envelope.Headers,
                    MessageSerializationContext.Empty);

            await _connector.RelayMessage(envelope);
            await _queue.Commit();

            (await _queue.GetLength()).Should().Be(1);
            var queued = (await _queue.Dequeue(1)).First();
            queued.EndpointName.Should().Be(envelope.Endpoint.Name);
            queued.Headers.Should().NotBeNull();
            queued.Headers!.Count.Should().Be(3);
            queued.Content.Should()
                .BeEquivalentTo(
                    new JsonMessageSerializer().Serialize(
                        envelope.Message,
                        envelope.Headers,
                        MessageSerializationContext.Empty));
        }

        [Fact]
        public async Task CommitRollback_ReceiveCommitReceiveRollback_FirstIsCommittedSecondIsDiscarded()
        {
            var envelope =
                new OutboundEnvelope<TestEventOne>(new TestEventOne(), null, TestProducerEndpoint.GetDefault());

            await _connector.RelayMessage(envelope);
            await _transactionManager.OnTransactionCompleted(new TransactionCompletedEvent());
            await _connector.RelayMessage(envelope);
            await _transactionManager.OnTransactionAborted(new TransactionAbortedEvent());

            (await _queue.GetLength()).Should().Be(1);
        }
    }
}
