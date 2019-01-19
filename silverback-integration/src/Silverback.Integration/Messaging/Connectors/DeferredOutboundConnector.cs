// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    // TODO: Test?
    /// <summary>
    /// Stores the message into a queue to be forwarded to the message broker later on.
    /// </summary>
    public class DeferredOutboundConnector : IOutboundConnector, ISubscriber
    {
        private readonly IOutboundQueueProducer _queueProducer;

        public DeferredOutboundConnector(IOutboundQueueProducer queueProducer)
        {
            _queueProducer = queueProducer;
        }

        [Subscribe]
        public Task OnTransactionCompleted(TransactionCompletedEvent message)
            => _queueProducer.Commit();

        [Subscribe]
        public Task OnTransactionAborted(TransactionAbortedEvent message)
            => _queueProducer.Rollback();

        public Task RelayMessage(IIntegrationMessage message, IEndpoint destinationEndpoint) =>
            _queueProducer.Enqueue(message, destinationEndpoint);
    }
}