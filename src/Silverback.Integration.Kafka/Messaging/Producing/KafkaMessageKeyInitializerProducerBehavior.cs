// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Sets the Kafka key with the value from the properties decorated with the <see cref="KafkaKeyMemberAttribute" />.
/// </summary>
public class KafkaMessageKeyInitializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageIdInitializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Producer is KafkaProducer && !context.Envelope.Headers.Contains(DefaultMessageHeaders.MessageId))
        {
            string? key = GetKafkaKey(context);

            if (key != null)
                context.Envelope.SetKafkaKey(key);
        }

        return next(context, cancellationToken);
    }

    private static string? GetKafkaKey(ProducerPipelineContext context)
    {
        string? keyFromMessage = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);

        // Ensure a key is set if the message is chunked to make sure all chunks are produced to the same partition
        if (keyFromMessage == null && context.Envelope.EndpointConfiguration.Chunk != null)
            return Guid.NewGuid().ToString();

        return keyFromMessage;
    }
}
