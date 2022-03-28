// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Sets the message key header with the value from the properties decorated with the
///     <see cref="KafkaKeyMemberAttribute" />. The header will be used by the
///     <see cref="Messaging.Broker.KafkaProducer" /> to set the actual message key.
/// </summary>
public class KafkaMessageKeyInitializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.BrokerKeyHeaderInitializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (!context.Envelope.Headers.Contains(KafkaMessageHeaders.KafkaMessageKey))
        {
            string key = GetKafkaKey(context);
            context.Envelope.Headers.Add(KafkaMessageHeaders.KafkaMessageKey, key);
        }

        return next(context);
    }

    private static string GetKafkaKey(ProducerPipelineContext context)
    {
        string? keyFromMessage = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);
        if (keyFromMessage != null)
            return keyFromMessage;

        string? messageIdHeaderValue = context.Envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
        if (messageIdHeaderValue != null)
            return messageIdHeaderValue;

        return Guid.NewGuid().ToString();
    }
}
