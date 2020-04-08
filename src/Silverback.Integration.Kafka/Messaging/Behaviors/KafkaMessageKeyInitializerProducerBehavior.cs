// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Behaviors
{
    /// <summary>
    ///     Sets the message key header with the value from the properties decorated with the
    ///     <see cref="KafkaKeyMemberAttribute"/>.
    ///     The header will be used by the <see cref="Messaging.Broker.KafkaProducer"/> to set
    ///     the actual message key.
    /// </summary>
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