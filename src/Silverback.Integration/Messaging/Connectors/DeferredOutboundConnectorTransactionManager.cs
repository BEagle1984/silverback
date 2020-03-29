// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    public class DeferredOutboundConnectorTransactionManager : ISubscriber
    {
        private readonly IOutboundQueueProducer _queueProducer;

        public DeferredOutboundConnectorTransactionManager(IOutboundQueueProducer queueProducer)
        {
            _queueProducer = queueProducer;
        }

        [Subscribe]
        public async Task OnTransactionCompleted(TransactionCompletedEvent message) => await _queueProducer.Commit();

        [Subscribe]
        public async Task OnTransactionAborted(TransactionAbortedEvent message) => await _queueProducer.Rollback();
    }
}