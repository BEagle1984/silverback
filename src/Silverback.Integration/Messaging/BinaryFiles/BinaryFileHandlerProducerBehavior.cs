// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Switches to the <see cref="BinaryFileMessageSerializer" /> if the message being produced implements
    ///     the <see cref="IBinaryFileMessage" /> interface.
    /// </summary>
    public class BinaryFileHandlerProducerBehavior : IProducerBehavior
    {
        private readonly BinaryFileMessageSerializer _binaryFileMessageSerializer = new BinaryFileMessageSerializer();

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.BinaryFileHandler;

        /// <inheritdoc cref="IProducerBehavior.Handle" />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Message is IBinaryFileMessage &&
                !(context.Envelope.Endpoint.Serializer is BinaryFileMessageSerializer))
            {
                context.Envelope.RawMessage = await _binaryFileMessageSerializer.SerializeAsync(
                    context.Envelope.Message,
                    context.Envelope.Headers,
                    MessageSerializationContext.Empty);
            }

            await next(context);
        }
    }
}
