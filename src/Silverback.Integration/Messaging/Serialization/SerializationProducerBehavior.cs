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
        public async Task Handle(IOutboundEnvelope envelope, OutboundEnvelopeHandler next)
        {
            ((OutboundEnvelope) envelope).RawMessage =
                await envelope.Endpoint.Serializer.SerializeAsync(envelope.Message, envelope.Headers,
                    new MessageSerializationContext(envelope.Endpoint));

            await next(envelope);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;
    }
}