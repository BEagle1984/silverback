// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Switches to the <see cref="BinaryFileMessageSerializer"/> if the message being consumed is a
    ///     binary message (according to the x-message-type header).
    /// </summary>
    public class BinaryFileHandlerConsumerBehavior : IConsumerBehavior, ISorted
    {
        /// <inheritdoc />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.BinaryFileHandler;

        /// <inheritdoc />
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            context.Envelopes = (await context.Envelopes.SelectAsync(Handle)).ToList();

            await next(context, serviceProvider);
        }

        private async Task<IRawInboundEnvelope> Handle(IRawInboundEnvelope envelope)
        {
            if (envelope.Endpoint.Serializer is BinaryFileMessageSerializer)
                return envelope;

            var messageType = SerializationHelper.GetTypeFromHeaders<object>(envelope.Headers);
            if (messageType == null || !typeof(IBinaryFileMessage).IsAssignableFrom(messageType))
                return envelope;

            var (deserializedObject, deserializedType) = await BinaryFileMessageSerializer.Default.DeserializeAsync(
                envelope.RawMessage,
                envelope.Headers,
                MessageSerializationContext.Empty);

            // Create typed message for easier specific subscription
            return SerializationHelper.CreateTypedInboundEnvelope(envelope, deserializedObject, deserializedType);
        }
    }
}
