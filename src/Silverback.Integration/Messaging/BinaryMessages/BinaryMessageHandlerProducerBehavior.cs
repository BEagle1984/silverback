// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Switches to the <see cref="BinaryMessageSerializer" /> if the message being produced implements the
///     <see cref="IBinaryMessage" /> interface.
/// </summary>
public class BinaryMessageHandlerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.BinaryMessageHandler;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope is { Message: IBinaryMessage, Endpoint.Configuration.Serializer: not IBinaryMessageSerializer })
        {
            context.Envelope.RawMessage = await DefaultSerializers.Binary.SerializeAsync(
                    context.Envelope.Message,
                    context.Envelope.Headers,
                    context.Envelope.Endpoint)
                .ConfigureAwait(false);
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
