// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     It ensures that an x-message-id header is always produced.
    /// </summary>
    public class MessageIdInitializerProducerBehavior : IProducerBehavior, ISorted
    {
        private readonly MessageIdProvider _messageIdProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageIdInitializerProducerBehavior" /> class.
        /// </summary>
        /// <param name="messageIdProvider">
        ///     The <see cref="MessageIdProvider" /> to be used to derive or generate a unique message id.
        /// </param>
        public MessageIdInitializerProducerBehavior(MessageIdProvider messageIdProvider)
        {
            _messageIdProvider = messageIdProvider;
        }

        /// <inheritdoc />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageIdInitializer;

        /// <inheritdoc />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            _messageIdProvider.EnsureMessageIdIsInitialized(context.Envelope.Message, context.Envelope.Headers);

            await next(context);
        }
    }
}
