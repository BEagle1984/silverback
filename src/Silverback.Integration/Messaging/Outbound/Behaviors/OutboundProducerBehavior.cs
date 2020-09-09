// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Behaviors
{
    /// <summary>
    ///     Produces the <see cref="IOutboundEnvelope{TMessage}" /> through the correct
    ///     <see cref="IOutboundConnector" />.
    /// </summary>
    public class OutboundProducerBehavior : IBehavior, ISorted
    {
        private readonly IReadOnlyCollection<IOutboundConnector> _outboundConnectors;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundProducerBehavior" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        public OutboundProducerBehavior(IServiceProvider serviceProvider)
        {
            _outboundConnectors = serviceProvider.GetServices<IOutboundConnector>().ToList();
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundProducer;

        /// <inheritdoc cref="IBehavior.Handle" />
        public async Task<IReadOnlyCollection<object>> Handle(
            IReadOnlyCollection<object> messages,
            MessagesHandler next)
        {
            Check.NotNull(next, nameof(next));

            await messages.OfType<IOutboundEnvelopeInternal>()
                .ForEachAsync(
                    outboundMessage => _outboundConnectors
                        .GetConnectorInstance(outboundMessage.OutboundConnectorType)
                        .RelayMessage(outboundMessage))
                .ConfigureAwait(false);

            return await next(messages).ConfigureAwait(false);
        }
    }
}
