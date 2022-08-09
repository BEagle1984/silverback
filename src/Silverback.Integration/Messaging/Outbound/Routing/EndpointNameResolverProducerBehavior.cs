// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Resolves the actual target endpoint name for the message being published using the
    ///     <see cref="IProducerEndpoint.GetActualName" /> method.
    /// </summary>
    public class EndpointNameResolverProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.EndpointNameResolver;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope is OutboundEnvelope outboundEnvelope)
            {
                string? actualEndpointName = outboundEnvelope.Endpoint.GetActualName(
                    outboundEnvelope,
                    context.ServiceProvider);

                if (actualEndpointName == null)
                    return;

                outboundEnvelope.ActualEndpointName = actualEndpointName;
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
