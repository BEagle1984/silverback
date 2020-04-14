// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     It ensures that an x-message-id header is always produced.
    /// </summary>
    public class MessageIdInitializerProducerBehavior : IProducerBehavior, ISorted
    {
        private readonly MessageIdProvider _messageIdProvider;

        public MessageIdInitializerProducerBehavior(MessageIdProvider messageIdProvider)
        {
            _messageIdProvider = messageIdProvider;
        }

        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            _messageIdProvider.EnsureMessageIdIsInitialized(context.Envelope.Message, context.Envelope.Headers);

            await next(context);
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageIdInitializer;
    }
}