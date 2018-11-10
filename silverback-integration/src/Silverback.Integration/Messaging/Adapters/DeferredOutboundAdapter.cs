using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Repositories;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Adapters
{
    // TODO: Test?
    /// <summary>
    /// Stores the <see cref="IMessage" /> into a queue to be forwarded to the message broker later on.
    /// </summary>
    /// <seealso cref="IOutboundAdapter" />
    public class DeferredOutboundAdapter : IOutboundAdapter
    {
        private readonly IOutboundQueueWriter _queueWriter;

        public DeferredOutboundAdapter(IOutboundQueueWriter queueWriter)
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