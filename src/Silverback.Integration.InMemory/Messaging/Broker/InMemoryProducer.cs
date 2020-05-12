// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer" />
    public class InMemoryProducer : Producer<InMemoryBroker, IProducerEndpoint>
    {
        public InMemoryProducer(
            InMemoryBroker broker,
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            ILogger<Producer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
        }

        protected override IOffset ProduceCore(IRawOutboundEnvelope envelope) =>
            Broker.GetTopic(Endpoint.Name).Publish(envelope.RawMessage, envelope.Headers);

        protected override Task<IOffset> ProduceAsyncCore(IRawOutboundEnvelope envelope) =>
            Broker.GetTopic(Endpoint.Name).PublishAsync(envelope.RawMessage, envelope.Headers);
    }
}