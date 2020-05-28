// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer" />
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
        ///     The <see cref="ILogger" />.
        /// </param>
        public InMemoryProducer(
            InMemoryBroker broker,
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            ILogger<Producer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
        }

        /// <inheritdoc />
        protected override IOffset? ProduceCore(IRawOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return Broker.GetTopic(Endpoint.Name).Publish(envelope.RawMessage, envelope.Headers);
        }

        /// <inheritdoc />
        protected override Task<IOffset?> ProduceAsyncCore(IRawOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return Broker.GetTopic(Endpoint.Name).PublishAsync(envelope.RawMessage, envelope.Headers);
        }
    }
}
