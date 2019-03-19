// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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
        public async Task OnTransactionCompleted(TransactionCompletedEvent message)
        {
            await _queueProducer.Commit();
        }

        [Subscribe]
        public async Task OnTransactionAborted(TransactionAbortedEvent message)
        {
            await _queueProducer.Rollback();
        }

        public async Task RelayMessage(object message, IEnumerable<MessageHeader> headers, IEndpoint destinationEndpoint)
        {
            _messageLogger.LogTrace(_logger, "Queuing message for deferred publish.", message, destinationEndpoint);
            await _queueProducer.Enqueue(message, headers, destinationEndpoint);
        }
    }
}