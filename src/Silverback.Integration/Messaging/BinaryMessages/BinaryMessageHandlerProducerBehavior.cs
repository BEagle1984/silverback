// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Switches to the <see cref="BinaryMessageSerializer{TModel}" /> if the message being produced implements the
///     <see cref="IBinaryMessage" /> interface.
/// </summary>
public class BinaryMessageHandlerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.BinaryMessageHandler;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope.Message is IBinaryMessage && context.Envelope.Endpoint.Configuration.Serializer is not IBinaryMessageSerializer)
        {
            context.Envelope.RawMessage = await DefaultSerializers.Binary.SerializeAsync(
                    context.Envelope.Message,
                    context.Envelope.Headers,
                    context.Envelope.Endpoint)
                .ConfigureAwait(false);
        }

        await next(context).ConfigureAwait(false);
    }
}
