// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public InMemoryProducer(
            InMemoryBroker broker,
            IProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Producer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
        }

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IOffset? ProduceCore(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var topic = Broker.GetTopic(Endpoint.Name);
            var messageBuffer = envelope.RawMessage.ReadAll();
            return topic.Publish(messageBuffer, envelope.Headers.Clone());
        }

        /// <inheritdoc cref="Producer.ProduceAsyncCore" />
        protected override async Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var topic = Broker.GetTopic(Endpoint.Name);
            var messageBuffer = await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false);

            return await topic.PublishAsync(messageBuffer, envelope.Headers.Clone()).ConfigureAwait(false);
        }
    }
}
