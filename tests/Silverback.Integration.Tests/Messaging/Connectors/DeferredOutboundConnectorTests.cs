// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
    public class DeferredOutboundConnectorTests
    {
        private readonly InMemoryOutboundQueue _queue;
        private readonly DeferredOutboundConnector _connector;
        private readonly DeferredOutboundConnectorTransactionManager _transactionManager;

        public DeferredOutboundConnectorTests()
        {
            _queue = new InMemoryOutboundQueue();
            _connector = new DeferredOutboundConnector(_queue, new NullLogger<DeferredOutboundConnector>(),
                new MessageLogger());
            _transactionManager = new DeferredOutboundConnectorTransactionManager(_queue);
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Queued()
        {
            var envelope = new OutboundEnvelope<TestEventOne>(
                new TestEventOne { Content = "Test" },
                new[]
                {
                    new MessageHeader("header1", "value1"),
                    new MessageHeader("header2", "value2")
                },
                TestProducerEndpoint.GetDefault());
            envelope.RawMessage =
                new JsonMessageSerializer().Serialize(envelope.Message, envelope.Headers);

            await _connector.RelayMessage(envelope);
            await _queue.Commit();

            (await _queue.GetLength()).Should().Be(1);
            var queued = (await _queue.Dequeue(1)).First();
            queued.Endpoint.Should().Be(envelope.Endpoint);
            queued.Headers.Count().Should().Be(3);
            queued.Content.Should()
                .BeEquivalentTo(
                    new JsonMessageSerializer().Serialize(envelope.Message, envelope.Headers));
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