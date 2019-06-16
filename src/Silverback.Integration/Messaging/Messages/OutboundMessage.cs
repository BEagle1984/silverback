// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Messages
{
    internal class OutboundMessage<TMessage> : IOutboundMessage<TMessage>, IOutboundMessageInternal
    {
        public IEndpoint Endpoint { get; set; }

        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        public TMessage Message { get; set; }

        public IOutboundRoute Route { get; set; }

        object IOutboundMessage.Message
        {
            get => Message;
            set => Message = (TMessage)value;
        }
    }
}