// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
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
                new MessageLogger(new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()})));
            _transactionManager = new DeferredOutboundConnectorTransactionManager(_queue);
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Queued()
        {
            var outboundMessage = new OutboundMessage<TestEventOne>()
            {
                Message = new TestEventOne {Content = "Test"},
                Headers =
                {
                    { "header1", "value1"},
                    { "header2", "value2"}
                },
                Endpoint = TestEndpoint.Default
            };

            await _connector.RelayMessage(outboundMessage);
            await _queue.Commit();

            _queue.Length.Should().Be(1);
            var queued = _queue.Dequeue(1).First();
            queued.Message.Endpoint.Should().Be(outboundMessage.Endpoint);
            queued.Message.Headers.Count.Should().Be(2);
            ((IIntegrationMessage)queued.Message.Message).Id.Should().Be(outboundMessage.Message.Id);
        }

        [Fact]
        public async Task CommitRollback_ReceiveCommitReceiveRollback_FirstIsCommittedSecondIsDiscarded()
        {
            var outboundMessage = new OutboundMessage<TestEventOne>()
            {
                Message = new TestEventOne(),
                Endpoint = TestEndpoint.Default
            };

            await _connector.RelayMessage(outboundMessage);
            await _transactionManager.OnTransactionCompleted(new TransactionCompletedEvent());
            await _connector.RelayMessage(outboundMessage);
            await _transactionManager.OnTransactionAborted(new TransactionAbortedEvent());

            _queue.Length.Should().Be(1);
        }
    }
}
