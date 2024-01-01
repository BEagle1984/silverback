// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
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

        if (context.Producer is KafkaProducer && !context.Envelope.Headers.Contains(KafkaMessageHeaders.KafkaMessageKey))
        {
            string? key = GetKafkaKey(context);

            if (key != null)
                context.Envelope.Headers.Add(KafkaMessageHeaders.KafkaMessageKey, key);
        }

        return next(context);
    }

    private static string? GetKafkaKey(ProducerPipelineContext context)
    {
        string? keyFromMessage = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);

        // Ensure a key is set if the message is chunked to make sure all chunks are produced to the same partition
        if (keyFromMessage == null && context.Envelope.Endpoint.Configuration.Chunk != null)
            return Guid.NewGuid().ToString();

        return keyFromMessage;
    }
}
