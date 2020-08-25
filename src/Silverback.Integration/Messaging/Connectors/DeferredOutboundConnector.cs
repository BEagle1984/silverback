// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Stores the outbound messages into a queue to be forwarded to the message broker by the
    ///     <see cref="IOutboundQueueWorker" />.
    /// </summary>
    public class DeferredOutboundConnector : IOutboundConnector
    {
        private readonly OutboundQueueBroker _outboundQueueBroker;

        private readonly ISilverbackIntegrationLogger _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeferredOutboundConnector" /> class.
        /// </summary>
        /// <param name="outboundQueueBroker">
        ///     The <see cref="OutboundQueueProducer"/> to be used to enqueue the messages using the underlying <see cref="IOutboundQueueWriter"/>.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public DeferredOutboundConnector(
            OutboundQueueBroker outboundQueueBroker,
            ISilverbackIntegrationLogger<DeferredOutboundConnector> logger)
        {
            _outboundQueueBroker = Check.NotNull(outboundQueueBroker, nameof(outboundQueueBroker));
            _logger = Check.NotNull(logger, nameof(logger));
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
        public Task RelayMessage(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            _logger.LogDebugWithMessageInfo(
                IntegrationEventIds.OutboundMessageEnqueued,
                "Enqueuing outbound message for deferred produce.",
                envelope);

            return _outboundQueueBroker.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
        }
    }
}
