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

namespace Silverback.Messaging.Outbound.Routing
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
                    outboundMessage =>
                        GetConnectorInstance(_outboundConnectors, outboundMessage.OutboundConnectorType)
                            .RelayMessage(outboundMessage))
                .ConfigureAwait(false);

            return await next(messages).ConfigureAwait(false);
        }

        private static TConnector GetConnectorInstance<TConnector>(
            IReadOnlyCollection<TConnector> connectors,
            Type? connectorType)
            where TConnector : class
        {
            Check.NotEmpty(connectors, nameof(connectors));

            if (connectorType == null)
            {
                return connectors.First();
            }

            return connectors.FirstOrDefault(connector => connector.GetType() == connectorType) ??
                   connectors.FirstOrDefault(connector => connectorType.IsInstanceOfType(connector)) ??
                   throw new InvalidOperationException(
                       $"No instance of {connectorType.Name} could be found in the collection of available connectors.");
        }
    }
}
