// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            context.Envelope = await DeserializeAsync(context).ConfigureAwait(false);

            await next(context).ConfigureAwait(false);
        }

        private static async Task<IRawInboundEnvelope> DeserializeAsync(ConsumerPipelineContext context)
        {
            var envelope = context.Envelope;

            if (envelope is IInboundEnvelope inboundEnvelope && inboundEnvelope.Message != null)
                return inboundEnvelope;

            var (deserializedObject, deserializedType) = await
                envelope.Endpoint.Serializer.DeserializeAsync(
                        envelope.RawMessage,
                        envelope.Headers,
                        new MessageSerializationContext(envelope.Endpoint, envelope.ActualEndpointName))
                    .ConfigureAwait(false);

            envelope.Headers.AddIfNotExists(DefaultMessageHeaders.MessageType, deserializedType.AssemblyQualifiedName);

            // Create typed message for easier specific subscription
            return SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
        }
    }
}
