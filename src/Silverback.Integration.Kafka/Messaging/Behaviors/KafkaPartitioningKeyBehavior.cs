// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Behaviors
{
    public class KafkaPartitioningKeyBehavior : IBehavior, ISorted
    {
        public int SortIndex { get; } = 200;

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            messages.OfType<IOutboundMessage>().ForEach(SetPartitioningKey);
            return next(messages);
        }
        
        private void SetPartitioningKey(IOutboundMessage outboundMessage)
        {
            var key = KafkaKeyHelper.GetMessageKey(outboundMessage.Content);

            if (key == null)
                return;

            outboundMessage.Headers.AddOrReplace(KafkaProducer.PartitioningKeyHeaderKey, key);
        }
    }
}