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
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            var key = RoutingKeyHelper.GetRoutingKey(context.Envelope.Message);

            if (key != null)
                context.Envelope.Headers.AddOrReplace(RabbitProducer.RoutingKeyHeaderKey, key);

            await next(context);
        }

        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;
    }
}