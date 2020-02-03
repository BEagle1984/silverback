// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Behaviors
{
    public class RabbitRoutingKeyBehavior : IBehavior, ISorted
    {
        public int SortIndex { get; } = 200;

        public Task<IReadOnlyCollection<object>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
        {
            messages.OfType<IOutboundEnvelope>().ForEach(SetRoutingKey);
            return next(messages);
        }

        private void SetRoutingKey(IOutboundEnvelope envelope)
        {
            var key = RoutingKeyHelper.GetRoutingKey(envelope.Message);

            if (key == null)
                return;

            envelope.Headers.AddOrReplace(RabbitProducer.RoutingKeyHeaderKey, key);
        }
    }
}