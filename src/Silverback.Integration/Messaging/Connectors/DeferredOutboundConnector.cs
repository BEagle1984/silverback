// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Stores the message into a queue to be forwarded to the message broker later on.
    /// </summary>
    public class DeferredOutboundConnector : IOutboundConnector
    {
        private readonly IOutboundQueueProducer _queueProducer;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        public DeferredOutboundConnector(
            IOutboundQueueProducer queueProducer,
            ILogger<DeferredOutboundConnector> logger,
            MessageLogger messageLogger)
        {
            _queueProducer = queueProducer;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public async Task RelayMessage(IOutboundEnvelope envelope)
        {
            _messageLogger.LogDebug(_logger, "Queuing message for deferred publish.", envelope);
            await _queueProducer.Enqueue(envelope);
        }
    }
}