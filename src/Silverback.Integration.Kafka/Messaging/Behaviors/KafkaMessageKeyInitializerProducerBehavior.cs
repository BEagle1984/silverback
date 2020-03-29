// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Behaviors
{
    public class KafkaMessageKeyInitializerProducerBehavior : IProducerBehavior, ISorted
    {
        public async Task Handle(IOutboundEnvelope envelope, OutboundEnvelopeHandler next)
        {
            var key = KafkaKeyHelper.GetMessageKey(envelope.Message);

            if (key != null)
                envelope.Headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, key);

            await next(envelope);
        }

        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;
    }
}