// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Connectors.Repositories
{
    [Collection("StaticInMemory")]
    public class InMemoryOutboundQueueTests
    {
        private readonly InMemoryOutboundQueue _queue;

        public InMemoryOutboundQueueTests()
        {
            _queue = new InMemoryOutboundQueue();
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public void EnqueueTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            });

            _queue.Length.Should().Be(0);
        }

        [Fact]
        public void EnqueueCommitTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            });

            _queue.Commit();

            _queue.Length.Should().Be(3);
        }

        [Fact]
        public void EnqueueRollbackTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            });

            _queue.Rollback();

            _queue.Length.Should().Be(0);
        }

        [Fact]
        public void EnqueueCommitRollbackCommitTest()
        {
            _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            _queue.Commit();
            _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            _queue.Rollback();
            _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            _queue.Commit();

            _queue.Length.Should().Be(2);
        }

        [Theory]
        [InlineData(3, 3)]
        [InlineData(5, 5)]
        [InlineData(10, 5)]
        public void DequeueTest(int count, int expected)
        {
            for (var i = 0; i < 5; i++)
            {
                _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            }

            _queue.Commit();

            var result = _queue.Dequeue(count);

            result.Count().Should().Be(expected);
        }

        [Fact]
        public void AcknowledgeRetryTest()
        {
            for (var i = 0; i < 5; i++)
            {
                _queue.Enqueue(new TestEventOne(), null, TestEndpoint.Default);
            }

            _queue.Commit();

            var result = _queue.Dequeue(5).ToArray();

            _queue.Acknowledge(result[0]);
            _queue.Retry(result[1]);
            _queue.Acknowledge(result[2]);
            _queue.Retry(result[3]);
            _queue.Acknowledge(result[4]);

            _queue.Length.Should().Be(2);
        }
    }
}
