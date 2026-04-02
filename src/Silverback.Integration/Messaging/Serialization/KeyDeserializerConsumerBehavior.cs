// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the messages being consumed using the configured <see cref="IMessageKeyDeserializer" />.
/// </summary>
public class KeyDeserializerConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.KeyDeserializer;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        IRawInboundEnvelope envelope = context.Envelope;

        if (envelope.Endpoint.Configuration is { KeyDeserializer: { } deserializer } && envelope.RawKey != null)
        {
            string? key = await deserializer.DeserializeAsync(envelope.RawKey, envelope.Headers, envelope.Endpoint).ConfigureAwait(false);

            if (key != null)
                envelope.Headers.AddOrReplace(DefaultMessageHeaders.MessageKey, key);
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
