// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Testing
{
    /// <summary>
    ///     Added at the very beginning of the producer pipeline, forwards the published
    ///     <see cref="IOutboundEnvelope" /> to the <see cref="IIntegrationSpy" />.
    /// </summary>
    public class OutboundSpyBrokerBehavior : IProducerBehavior
    {
        private readonly IntegrationSpy _integrationSpy;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundSpyBrokerBehavior" /> class.
        /// </summary>
        /// <param name="integrationSpy">
        ///     The <see cref="IntegrationSpy" />.
        /// </param>
        public OutboundSpyBrokerBehavior(IntegrationSpy integrationSpy)
        {
            _integrationSpy = Check.NotNull(integrationSpy, nameof(integrationSpy));
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex { get; } = int.MinValue;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            _integrationSpy.AddOutboundEnvelope(context.Envelope);

            return next(context);
        }
    }
}
