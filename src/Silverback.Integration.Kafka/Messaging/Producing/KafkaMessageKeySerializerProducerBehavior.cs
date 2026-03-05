// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Serializes the Kafka message key using the <see cref="IMessageKeySerializer" /> specified
///     on the <see cref="KafkaProducerEndpointConfiguration" /> if available. The string
///     key value is extracted in an earlier step by <see cref="KafkaMessageKeyInitializerProducerBehavior" />.
/// </summary>
public class KafkaMessageKeySerializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.KeySerializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Producer.EndpointConfiguration is { KeySerializer: { } keySerializer })
        {
            string? key = context.Envelope.GetKafkaKey();
            if (key != null)
            {
                Stream? serializedKey = await keySerializer.SerializeAsync(
                        key,
                        context.Envelope.Headers,
                        context.Envelope.GetEndpoint())
                    .ConfigureAwait(false);
                context.Envelope.RawKey = serializedKey;
            }
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
