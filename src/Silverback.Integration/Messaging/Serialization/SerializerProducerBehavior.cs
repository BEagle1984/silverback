// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes the message being produced using the configured <see cref="IMessageSerializer" />.
    /// </summary>
    public class SerializerProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            context.Envelope.RawMessage ??=
                await context.Envelope.Endpoint.Serializer.SerializeAsync(
                        context.Envelope.Message,
                        context.Envelope.Headers,
                        new MessageSerializationContext(context.Envelope.Endpoint))
                    .ConfigureAwait(false);

            await next(context).ConfigureAwait(false);
        }
    }
}
