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
    public class KafkaMessageKeyBehavior : IBehavior, ISorted
    {
        public int SortIndex { get; } = 100;

        public Task<IReadOnlyCollection<object>> Handle(IReadOnlyCollection<object> messages, MessagesHandler next)
        {
            messages.OfType<IOutboundEnvelope>().ForEach(SetPartitioningKey);
            return next(messages);
        }

        private void SetPartitioningKey(IOutboundEnvelope envelope)
        {
            var key = KafkaKeyHelper.GetMessageKey(envelope.Message);

            if (key == null)
                return;

            envelope.Headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, key);
        }
    }
}