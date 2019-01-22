// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        public DeferredOutboundConnector(IOutboundQueueProducer queueProducer, ILogger<DeferredOutboundConnector> logger, MessageLogger messageLogger)
        {
            _queueProducer = queueProducer;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        [Subscribe]
        public Task OnTransactionCompleted(TransactionCompletedEvent message) => _queueProducer.Commit();

        [Subscribe]
        public Task OnTransactionAborted(TransactionAbortedEvent message) => _queueProducer.Rollback();

        public Task RelayMessage(object message, IEndpoint destinationEndpoint)
        {
            _messageLogger.LogTrace(_logger, "Queuing message for deferred publish.", message, destinationEndpoint);
            return _queueProducer.Enqueue(message, destinationEndpoint);
        }
    }
}