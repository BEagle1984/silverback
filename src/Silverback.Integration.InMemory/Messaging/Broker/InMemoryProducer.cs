// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class InMemoryProducer : Producer<InMemoryBroker, IProducerEndpoint>
    {
        public InMemoryProducer(
            InMemoryBroker broker,
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
            : base(broker, endpoint, behaviors, logger, messageLogger)
        {
        }

        /// <inheritdoc cref="Producer" />
        protected override IOffset ProduceImpl(IRawOutboundEnvelope envelope) =>
            Broker.GetTopic(Endpoint.Name).Publish(envelope.RawMessage, envelope.Headers);

        /// <inheritdoc cref="Producer" />
        protected override Task<IOffset> ProduceAsyncImpl(IRawOutboundEnvelope envelope) =>
            Broker.GetTopic(Endpoint.Name).PublishAsync(envelope.RawMessage, envelope.Headers);
    }
}