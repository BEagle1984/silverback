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
    ///     <see cref="IOutboundConnector" /> instance.
    /// </summary>
    public class OutboundProducerBehavior : IBehavior, ISorted
    {
        private readonly IReadOnlyCollection<IOutboundConnector> _outboundConnectors;

        public OutboundProducerBehavior(IServiceProvider serviceProvider)
        {
            _outboundConnectors = serviceProvider.GetServices<IOutboundConnector>().ToList();
        }

        public async Task<IReadOnlyCollection<object>> Handle(
            IReadOnlyCollection<object> messages,
            MessagesHandler next)
        {
            await messages.OfType<IOutboundEnvelopeInternal>()
                .ForEachAsync(outboundMessage => _outboundConnectors
                    .GetConnectorInstance(outboundMessage.OutboundConnectorType)
                    .RelayMessage(outboundMessage));

            return await next(messages);
        }

        public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundProducer;
    }
}