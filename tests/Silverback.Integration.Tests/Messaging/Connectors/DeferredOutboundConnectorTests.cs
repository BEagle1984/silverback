// Copyright (c) 2018-2019 Sergio Aquilini
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
                new MessageLogger(new MessageKeyProvider(new[] {new DefaultPropertiesMessageKeyProvider()})));
            _transactionManager = new DeferredOutboundConnectorTransactionManager(_queue);
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public async Task OnMessageReceived_SingleMessage_Queued()
        {
            var outboundMessage = new OutboundMessage<TestEventOne>(
                new TestEventOne { Content = "Test" },
                new[]
                {
                    new MessageHeader("header1", "value1"),
                    new MessageHeader("header2", "value2")
                },
                TestEndpoint.Default);
            outboundMessage.RawContent = new JsonMessageSerializer().Serialize(outboundMessage.Content, outboundMessage.Headers);

            await _connector.RelayMessage(outboundMessage);
            await _queue.Commit();

            _queue.Length.Should().Be(1);
            var queued = (await _queue.Dequeue(1)).First();
            queued.Endpoint.Should().Be(outboundMessage.Endpoint);
            queued.Headers.Count().Should().Be(3);
            queued.Content.Should().BeEquivalentTo(new JsonMessageSerializer().Serialize(outboundMessage.Content, outboundMessage.Headers));
        }

        [Fact]
        public async Task CommitRollback_ReceiveCommitReceiveRollback_FirstIsCommittedSecondIsDiscarded()
        {
            var outboundMessage = new OutboundMessage<TestEventOne>(new TestEventOne(), null, TestEndpoint.Default);
 
            await _connector.RelayMessage(outboundMessage);
            await _transactionManager.OnTransactionCompleted(new TransactionCompletedEvent());
            await _connector.RelayMessage(outboundMessage);
            await _transactionManager.OnTransactionAborted(new TransactionAbortedEvent());

            _queue.Length.Should().Be(1);
        }
    }
}
