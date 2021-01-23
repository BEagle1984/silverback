// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound
{
    /// <summary>
    ///     Sets the routing key header with the value from the property decorated with the
    ///     <see cref="RabbitRoutingKeyAttribute" />. The header will be used by the
    ///     <see cref="Messaging.Broker.RabbitProducer" /> to set the actual routing key.
    /// </summary>
    public class RabbitRoutingKeyInitializerProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Message != null)
            {
                var key = RabbitRoutingKeyHelper.GetRoutingKey(context.Envelope.Message);

                if (key != null)
                {
                    context.Envelope.Headers.AddOrReplace(RabbitMessageHeaders.RoutingKey, key);
                    context.Envelope.AdditionalLogData["routingKey"] = key;
                }
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
