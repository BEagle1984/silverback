// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            string key = GetKafkaKey(context);

            context.Envelope.Headers.AddOrReplace(KafkaMessageHeaders.KafkaMessageKey, key);
            context.Envelope.AdditionalLogData["kafkaKey"] = key;

            await next(context).ConfigureAwait(false);
        }

        private static string GetKafkaKey(ProducerPipelineContext context)
        {
            string? kafkaKeyHeaderValue = context.Envelope.Headers.GetValue(KafkaMessageHeaders.KafkaMessageKey);
            if (kafkaKeyHeaderValue != null)
                return kafkaKeyHeaderValue;

            string? keyFromMessage = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);
            if (keyFromMessage != null)
                return keyFromMessage;

            var messageIdHeaderValue = context.Envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
            if (messageIdHeaderValue != null)
                return messageIdHeaderValue;

            return Guid.NewGuid().ToString();
        }
    }
}
