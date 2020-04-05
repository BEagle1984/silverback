// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes the message being produced using the configured <see cref="IMessageSerializer" />.
    /// </summary>
    public class SerializerProducerBehavior : IProducerBehavior, ISorted
    {
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            ((OutboundEnvelope) context.Envelope).RawMessage =
                await context.Envelope.Endpoint.Serializer.SerializeAsync(
                    context.Envelope.Message,
                    context.Envelope.Headers,
                    new MessageSerializationContext(context.Envelope.Endpoint));

            await next(context);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;
    }
}