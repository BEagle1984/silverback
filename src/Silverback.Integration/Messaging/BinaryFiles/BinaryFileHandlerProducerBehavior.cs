// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.BinaryFiles
{
    /// <summary>
    ///     Switches to the <see cref="BinaryFileMessageSerializer"/> if the message being produced implements
    ///     the <see cref="IBinaryFileMessage"/> interface.
    /// </summary>
    public class BinaryFileHandlerProducerBehavior : IProducerBehavior, ISorted
    {
        private readonly BinaryFileMessageSerializer _binaryFileMessageSerializer = new BinaryFileMessageSerializer();
        
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            if (context.Envelope.Message is IBinaryFileMessage)
            {
                context.Envelope.RawMessage = _binaryFileMessageSerializer.Serialize(
                    context.Envelope.Message,
                    context.Envelope.Headers,
                    MessageSerializationContext.Empty);
            }

            await next(context);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.BinaryFileHandler;
    }
}