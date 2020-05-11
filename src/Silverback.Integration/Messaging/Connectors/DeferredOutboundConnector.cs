// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Stores the outbound messages into a queue to be forwarded to the message broker by the
    ///     <see cref="IOutboundQueueWorker" />.
    /// </summary>
    public class DeferredOutboundConnector : IOutboundConnector
    {
        private readonly IOutboundQueueWriter _queueWriter;

        private readonly ILogger _logger;

        private readonly MessageLogger _messageLogger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeferredOutboundConnector" /> class.
        /// </summary>
        /// <param name="queueWriter">
        ///     The <see cref="IOutboundQueueWriter" /> implementation to be used to enqueue the messages.
        /// </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        /// <param name="messageLogger"> The <see cref="MessageLogger" />. </param>
        public DeferredOutboundConnector(
            IOutboundQueueWriter queueWriter,
            ILogger<DeferredOutboundConnector> logger,
            MessageLogger messageLogger)
        {
            _queueWriter = queueWriter;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        /// <summary>
        ///     Stores the message in the outbound queue to be forwarded to the message broker endpoint by the
        ///     <see cref="IOutboundQueueWorker" />.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be produced.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        public async Task RelayMessage(IOutboundEnvelope envelope)
        {
            _messageLogger.LogDebug(_logger, EventIds.DeferredOutboundConnectorEnqueueMessage, "Enqueuing outbound message for deferred produce.", envelope);
            await _queueWriter.Enqueue(envelope);
        }
    }
}
