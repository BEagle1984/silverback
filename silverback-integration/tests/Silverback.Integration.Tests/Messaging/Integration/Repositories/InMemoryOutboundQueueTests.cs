using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Silverback.Messaging;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Integration.Repositories
{
    [TestFixture]
    public class InMemoryOutboundQueueTests
    {
        private InMemoryOutboundQueue _queue;

        [SetUp]
        public void Setup()
        {
            _queue = new InMemoryOutboundQueue();
        }

        [Test]
        public void EnqueueTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            });

            Assert.That(_queue.Length, Is.EqualTo(0));
        }

        [Test]
        public void EnqueueCommitTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            });

            _queue.Commit();

            Assert.That(_queue.Length, Is.EqualTo(3));
        }

        [Test]
        public void EnqueueRollbackTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            });

            _queue.Rollback();

            Assert.That(_queue.Length, Is.EqualTo(0));
        }

        [Test]
        public void EnqueueCommitRollbackCommitTest()
        {
            _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            _queue.Commit();
            _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            _queue.Rollback();
            _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            _queue.Commit();

            Assert.That(_queue.Length, Is.EqualTo(2));
        }

        [Test]
        [TestCase(3, 3)]
        [TestCase(5, 5)]
        [TestCase(10, 5)]
        public void DequeueTest(int count, int expected)
        {
            for (var i = 0; i < 5; i++)
            {
                _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            }

            _queue.Commit();

            var result = _queue.Dequeue(count);

            Assert.That(result.Count(), Is.EqualTo(expected));
        }

        [Test]
        public void AcknowledgeRetryTest()
        {
            for (var i = 0; i < 5; i++)
            {
                _queue.Enqueue(new TestEventOne(), TestEndpoint.Default);
            }

            _queue.Commit();

            var result = _queue.Dequeue(5).ToArray();

            _queue.Acknowledge(result[0]);
            _queue.Retry(result[1]);
            _queue.Acknowledge(result[2]);
            _queue.Retry(result[3]);
            _queue.Acknowledge(result[4]);

            Assert.That(_queue.Length, Is.EqualTo(2));
        }
    }
}
