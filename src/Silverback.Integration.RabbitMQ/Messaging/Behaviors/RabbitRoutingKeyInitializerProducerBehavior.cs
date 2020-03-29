// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Behaviors
{
    public class RabbitRoutingKeyInitializerProducerBehavior : IProducerBehavior, ISorted
    {
        public async Task Handle(IOutboundEnvelope envelope, IProducer producer, OutboundEnvelopeHandler next)
        {
            var key = RoutingKeyHelper.GetRoutingKey(envelope.Message);

            if (key != null)
                envelope.Headers.AddOrReplace(RabbitProducer.RoutingKeyHeaderKey, key);

            await next(envelope, producer);
        }

        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;
    }
}