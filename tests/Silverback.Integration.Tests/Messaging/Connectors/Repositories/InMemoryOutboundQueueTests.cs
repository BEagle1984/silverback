// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    public class InMemoryOutboundQueueTests
    {
        private readonly InMemoryOutbox _queue;

        private readonly IOutboundEnvelope _sampleOutboundEnvelope = new OutboundEnvelope(
            new TestEventOne { Content = "Test" },
            null,
            TestProducerEndpoint.GetDefault());

        public InMemoryOutboundQueueTests()
        {
            _queue = new InMemoryOutbox(new TransactionalListSharedItems<OutboxStoredMessage>());
        }

        [Fact]
        public async Task Enqueue_MultipleTimesInParallelNoCommit_QueueLooksEmpty()
        {
            Parallel.For(0, 3, _ => { _queue.WriteAsync(_sampleOutboundEnvelope); });

            (await _queue.GetLengthAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Enqueue_MultipleTimesInParallelAndCommit_QueueFilled()
        {
            Parallel.For(0, 3, _ => { _queue.WriteAsync(_sampleOutboundEnvelope); });

            await _queue.CommitAsync();

            (await _queue.GetLengthAsync()).Should().Be(3);
        }

        [Fact]
        public async Task Enqueue_MultipleTimesInParallelAndRollback_QueueIsEmpty()
        {
            Parallel.For(0, 3, _ => { _queue.WriteAsync(_sampleOutboundEnvelope); });

            await _queue.RollbackAsync();

            (await _queue.GetLengthAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Enqueue_SomeEnvelopes_OnlyCommittedEnvelopesAreEnqueued()
        {
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.CommitAsync();
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.RollbackAsync();
            await _queue.WriteAsync(_sampleOutboundEnvelope);
            await _queue.CommitAsync();

            (await _queue.GetLengthAsync()).Should().Be(2);
        }

        [Theory]
        [InlineData(3, 3)]
        [InlineData(5, 5)]
        [InlineData(10, 5)]
        public async Task Dequeue_WithCommittedEnvelopes_ExpectedEnvelopesReturned(int count, int expected)
        {
            for (var i = 0; i < 5; i++)
            {
                await _queue.WriteAsync(_sampleOutboundEnvelope);
            }

            await _queue.CommitAsync();

            var result = await _queue.ReadAsync(count);

            result.Count.Should().Be(expected);
        }

        [Fact]
        public async Task AcknowledgeAndRetry_RetriedAreStillEnqueued()
        {
            for (var i = 0; i < 5; i++)
            {
                await _queue.WriteAsync(_sampleOutboundEnvelope);
            }

            await _queue.CommitAsync();

            var result = (await _queue.ReadAsync(5)).ToArray();

            await _queue.AcknowledgeAsync(result[0]);
            await _queue.RetryAsync(result[1]);
            await _queue.AcknowledgeAsync(result[2]);
            await _queue.RetryAsync(result[3]);
            await _queue.AcknowledgeAsync(result[4]);

            (await _queue.GetLengthAsync()).Should().Be(2);
        }
    }
}
