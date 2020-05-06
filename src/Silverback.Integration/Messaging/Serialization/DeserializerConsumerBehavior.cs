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
    public class DeserializerConsumerBehavior : IConsumerBehavior, ISorted
    {
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            context.Envelopes = (await context.Envelopes.SelectAsync(Deserialize)).ToList();

            await next(context, serviceProvider);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

        public static async Task<IRawInboundEnvelope> Deserialize(IRawInboundEnvelope envelope)
        {
            if (envelope is IInboundEnvelope inboundEnvelope && inboundEnvelope.Message != null)
                return envelope;

            var (deserializedObject, deserializedType) = await
                envelope.Endpoint.Serializer.DeserializeAsync(
                    envelope.RawMessage,
                    envelope.Headers,
                    new MessageSerializationContext(envelope.Endpoint, envelope.ActualEndpointName));

            // Create typed message for easier specific subscription
            return SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
        }
    }
}