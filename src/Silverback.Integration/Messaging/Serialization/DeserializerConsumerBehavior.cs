// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
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
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IServiceProvider serviceProvider,
            IConsumer consumer,
            RawInboundEnvelopeHandler next)
        {
            var newEnvelopes = await envelopes.SelectAsync(Deserialize);

            await next(newEnvelopes.ToList(), serviceProvider, consumer);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

        private async Task<IRawInboundEnvelope> Deserialize(IRawInboundEnvelope envelope)
        {
            if (envelope is IInboundEnvelope inboundEnvelope && inboundEnvelope.Message != null)
                return inboundEnvelope;

            var deserialized = await
                envelope.Endpoint.Serializer.DeserializeAsync(
                    envelope.RawMessage,
                    envelope.Headers,
                    new MessageSerializationContext(envelope.Endpoint, envelope.ActualEndpointName));

            if (deserialized == null)
                return envelope;

            // Create typed message for easier specific subscription
            var typedInboundMessage = (InboundEnvelope) Activator.CreateInstance(
                typeof(InboundEnvelope<>).MakeGenericType(deserialized.GetType()),
                envelope);

            typedInboundMessage.Message = deserialized;

            return typedInboundMessage;
        }
    }
}