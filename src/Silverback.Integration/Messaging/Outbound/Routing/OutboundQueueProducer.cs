// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Deferred;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class OutboundQueueProducer : Producer<TransactionalOutboxBroker, IProducerEndpoint>
    {
        private readonly IOutboundQueueWriter _queueWriter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundQueueProducer" /> class.
        /// </summary>
        /// <param name="queueWriter">
        ///     The <see cref="IOutboundQueueWriter"/> to be used to write to the queue.
        /// </param>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public OutboundQueueProducer(
            IOutboundQueueWriter queueWriter,
            TransactionalOutboxBroker broker,
            IProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Producer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            _queueWriter = queueWriter;
        }

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IOffset ProduceCore(IOutboundEnvelope envelope)
        {
            throw new InvalidOperationException("Only asynchronous operations are supported.");
        }

        /// <inheritdoc cref="Producer.ProduceAsyncCore" />
        protected override async Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            await _queueWriter.Enqueue(envelope).ConfigureAwait(false);

            return null;
        }
    }
}
