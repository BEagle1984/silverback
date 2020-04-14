// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    [Collection("StaticInMemory")]
    public class InMemoryOutboundQueueTests
    {
        private readonly InMemoryOutboundQueue _queue;

        private readonly IOutboundEnvelope _sampleOutboundEnvelope = new OutboundEnvelope(
            new TestEventOne { Content = "Test" }, null, TestProducerEndpoint.GetDefault());

        public InMemoryOutboundQueueTests()
        {
            _queue = new InMemoryOutboundQueue();
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public async Task Enqueue_MultipleTimesInParallelNoCommit_QueueLooksEmpty()
        {
            Parallel.For(0, 3, _ => { _queue.Enqueue(_sampleOutboundEnvelope); });

            (await _queue.GetLength()).Should().Be(0);
        }

        [Fact]
        public async Task Enqueue_MultipleTimesInParallelAndCommit_QueueFilled()
        {
            Parallel.For(0, 3, _ => { _queue.Enqueue(_sampleOutboundEnvelope); });

            await _queue.Commit();

            (await _queue.GetLength()).Should().Be(3);
        }

        [Fact]
        public async Task Enqueue_MultipleTimesInParallelAndRollback_QueueIsEmpty()
        {
            Parallel.For(0, 3, _ => { _queue.Enqueue(_sampleOutboundEnvelope); });

            await _queue.Rollback();

            (await _queue.GetLength()).Should().Be(0);
        }

        [Fact]
        public async Task Enqueue_SomeEnvelopes_OnlyCommittedEnvelopesAreEnqueued()
        {
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Commit();
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Rollback();
            await _queue.Enqueue(_sampleOutboundEnvelope);
            await _queue.Commit();

            (await _queue.GetLength()).Should().Be(2);
        }

        [Theory]
        [InlineData(3, 3)]
        [InlineData(5, 5)]
        [InlineData(10, 5)]
        public async Task Dequeue_WithCommittedEnvelopes_ExpectedEnvelopesReturned(int count, int expected)
        {
            for (var i = 0; i < 5; i++)
            {
                await _queue.Enqueue(_sampleOutboundEnvelope);
            }

            await _queue.Commit();

            var result = await _queue.Dequeue(count);

            result.Count.Should().Be(expected);
        }

        [Fact]
        public async Task AcknowledgeAndRetry_RetriedAreStillEnqueued()
        {
            for (var i = 0; i < 5; i++)
            {
                await _queue.Enqueue(_sampleOutboundEnvelope);
            }

            await _queue.Commit();

            var result = (await _queue.Dequeue(5)).ToArray();

            await _queue.Acknowledge(result[0]);
            await _queue.Retry(result[1]);
            await _queue.Acknowledge(result[2]);
            await _queue.Retry(result[3]);
            await _queue.Acknowledge(result[4]);

            (await _queue.GetLength()).Should().Be(2);
        }
    }
}