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
            messages.OfType<IOutboundMessage>().ForEach(SetPartitioningKey);
            return next(messages);
        }

        private void SetPartitioningKey(IOutboundMessage outboundMessage)
        {
            var key = RoutingKeyHelper.GetRoutingKey(outboundMessage.Content);

            if (key == null)
                return;

            outboundMessage.Headers.AddOrReplace(RabbitProducer.RoutingKeyHeaderKey, key);
        }
    }
}