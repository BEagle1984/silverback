// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Behaviors
{
    /// <summary>
    ///     Sets the message key header with the value from the properties decorated with the
    ///     <see cref="KafkaKeyMemberAttribute" />. The header will be used by the
    ///     <see cref="Messaging.Broker.KafkaProducer" /> to set the actual message key.
    /// </summary>
    public class KafkaMessageKeyInitializerProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;

        /// <inheritdoc cref="IProducerBehavior.Handle" />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            string? key = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);

            if (key != null)
            {
                context.Envelope.Headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, key);
                context.Envelope.AdditionalLogData["kafkaKey"] = key;
            }
            else
            {
                var messageId = context.Envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

                if (messageId != null)
                {
                    context.Envelope.Headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, messageId);
                    context.Envelope.AdditionalLogData["kafkaKey"] = messageId;
                }
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
