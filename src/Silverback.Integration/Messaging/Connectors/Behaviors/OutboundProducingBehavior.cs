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
    public class OutboundProducingBehavior : IBehavior, ISorted
    {
        private readonly IEnumerable<IOutboundConnector> _outboundConnectors;

        public OutboundProducingBehavior(IServiceProvider serviceProvider)
        {
            _outboundConnectors = serviceProvider.GetServices<IOutboundConnector>();
        }

        public int SortIndex { get; } = 100;

        public async Task<IReadOnlyCollection<object>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
        {
            await messages.OfType<IOutboundEnvelopeInternal>()
                .ForEachAsync(outboundMessage => _outboundConnectors
                    .GetConnectorInstance(outboundMessage.Route.OutboundConnectorType)
                    .RelayMessage(outboundMessage));

            return await next(messages);
        }
    }
}