// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Deferred;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
    /// <summary>
    ///     The messages are stored in a the transactional outbox table. The operation is therefore included in database
    ///     transaction used to update the data. The <see cref="OutboxTableWorker" /> takes care of asynchronously
    ///     sending the messages to the message broker.
    /// </summary>
    public class TransactionalOutboxProduceStrategy : IProduceStrategy
    {
        private readonly TransactionalOutboxBroker _outboundQueueBroker;

        private readonly ISilverbackIntegrationLogger _logger;
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransactionalOutboxProduceStrategy" /> class.
        /// </summary>
        /// <param name="outboundQueueBroker">
        ///     The <see cref="TransactionalOutboxBroker"/> to be used to enqueue the messages using the underlying <see cref="IOutboundQueueWriter"/>.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public TransactionalOutboxProduceStrategy(
            TransactionalOutboxBroker outboundQueueBroker,
            ISilverbackIntegrationLogger<DeferredOutboundConnector> logger)
        {
            _outboundQueueBroker = Check.NotNull(outboundQueueBroker, nameof(outboundQueueBroker));
            _logger = Check.NotNull(logger, nameof(logger));
        }
        public Task ProduceAsync(IOutboundEnvelope envelope)
        {
            throw new System.NotImplementedException();
        }
    }
}
