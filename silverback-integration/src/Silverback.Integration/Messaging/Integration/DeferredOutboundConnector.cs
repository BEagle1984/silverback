using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Integration.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Integration
{
    // TODO: Test?
    /// <summary>
    /// Stores the <see cref="IMessage" /> into a queue to be forwarded to the message broker later on.
    /// </summary>
    /// <seealso cref="IOutboundConnector" />
    public class DeferredOutboundConnector : IOutboundConnector
    {
        private readonly IOutboundQueueWriter _queueWriter;

        public DeferredOutboundConnector(IOutboundQueueWriter queueWriter)
        {
            _queueWriter = queueWriter;
        }

        public void Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
            => _queueWriter.Enqueue(message, endpoint);

        public Task RelayAsync(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
        {
            Relay(message, producer, endpoint);
            return Task.CompletedTask;
        }

        [Subscribe]
        public void OnTransactionCommit(TransactionCommitEvent message)
            => _queueWriter.Commit();

        [Subscribe]
        public void OnTransactionRollback(TransactionRollbackEvent message)
            => _queueWriter.Rollback();
    }
}