// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Deserializes the messages being consumed using the configured <see cref="IMessageSerializer" />.
    /// </summary>
    public class DeserializerConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            context.Envelope = await Deserialize(context.Envelope).ConfigureAwait(false);

            await next(context).ConfigureAwait(false);
        }

        private static async Task<IRawInboundEnvelope> Deserialize(IRawInboundEnvelope envelope)
        {
            if (envelope is IInboundEnvelope inboundEnvelope && inboundEnvelope.Message != null)
                return envelope;

            var (deserializedObject, deserializedType) = await
                envelope.Endpoint.Serializer.DeserializeAsync(
                        envelope.RawMessage,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint, envelope.ActualEndpointName))
                    .ConfigureAwait(false);

            // Create typed message for easier specific subscription
            return SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
        }
    }
}
