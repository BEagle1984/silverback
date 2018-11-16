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
    public class DeferredOutboundConnector : OutboundConnectorBase
    {
        private readonly IOutboundQueueWriter _queueWriter;

        public DeferredOutboundConnector(IOutboundQueueWriter queueWriter, IOutboundRoutingConfiguration routingConfiguration) : base(routingConfiguration)
        {
            _queueWriter = queueWriter;
        }

        [Subscribe]
        public Task OnTransactionCommit(TransactionCommitEvent message)
            => _queueWriter.Commit();

        [Subscribe]
        public Task OnTransactionRollback(TransactionRollbackEvent message)
            => _queueWriter.Rollback();

        protected override Task RelayMessage(IIntegrationMessage message, IEndpoint destinationEndpoint) =>
            _queueWriter.Enqueue(message, destinationEndpoint);
    }
}