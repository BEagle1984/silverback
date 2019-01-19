// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Connectors
{
    [Collection("StaticInMemory")]
    public class DeferredOutboundConnectorTests
    {
        private readonly InMemoryOutboundQueue _queue;
        private readonly DeferredOutboundConnector _connector;

        public DeferredOutboundConnectorTests()
        {
            _queue = new InMemoryOutboundQueue();
            _connector = new DeferredOutboundConnector(_queue);
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Queued()
        {
            var endpoint = TestEndpoint.Default;

            var message = new TestEventOne { Content = "Test" };

            await _connector.RelayMessage(message, endpoint);
            await _queue.Commit();

            _queue.Length.Should().Be(1);
            var queued = _queue.Dequeue(1).First();
            queued.Endpoint.Should().Be(endpoint);
            ((IIntegrationMessage)queued.Message).Id.Should().Be(message.Id);
        }

        [Fact]
        public void CommitRollback_ReceiveCommitReceiveRollback_FirstIsCommittedSecondIsDiscarded()
        {
            _connector.RelayMessage(new TestEventOne(), TestEndpoint.Default);
            _connector.OnTransactionCompleted(new TransactionCompletedEvent());
            _connector.RelayMessage(new TestEventOne(), TestEndpoint.Default);
            _connector.OnTransactionAborted(new TransactionAbortedEvent());

            _queue.Length.Should().Be(1);
        }
    }
}
