// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class InMemoryProducer : Producer<InMemoryBroker, IProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be added to the pipeline.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public InMemoryProducer(
            InMemoryBroker broker,
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            ISilverbackIntegrationLogger<Producer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
        }

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IOffset? ProduceCore(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return Broker.GetTopic(Endpoint.Name).Publish(envelope.RawMessage, envelope.Headers.Clone());
        }

        /// <inheritdoc cref="Producer.ProduceAsyncCore" />
        protected override Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return Broker.GetTopic(Endpoint.Name).PublishAsync(envelope.RawMessage, envelope.Headers.Clone());
        }
    }
}
