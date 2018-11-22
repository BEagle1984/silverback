using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    // TODO: Test?
    /// <summary>
    /// Stores the <see cref="IMessage" /> into a queue to be forwarded to the message broker later on.
    /// </summary>
    public class DeferredOutboundConnector : IOutboundConnector, ISubscriber
    {
        private readonly IOutboundQueueProducer _queueProducer;

        public DeferredOutboundConnector(IOutboundQueueProducer queueProducer)
        {
            _queueProducer = queueProducer;
        }

        [Subscribe]
        public Task OnTransactionCommit(TransactionCommitEvent message)
            => _queueProducer.Commit();

        [Subscribe]
        public Task OnTransactionRollback(TransactionRollbackEvent message)
            => _queueProducer.Rollback();

        public Task RelayMessage(IIntegrationMessage message, IEndpoint destinationEndpoint) =>
            _queueProducer.Enqueue(message, destinationEndpoint);
    }
}