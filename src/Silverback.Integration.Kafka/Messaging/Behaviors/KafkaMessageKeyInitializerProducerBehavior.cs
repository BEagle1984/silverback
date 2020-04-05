// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Behaviors
{
    public class KafkaMessageKeyInitializerProducerBehavior : IProducerBehavior, ISorted
    {
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            var key = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);

            if (key != null)
                context.Envelope.Headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, key);

            await next(context);
        }

        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;
    }
}