// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Messages
{
    public class InboundMessage<TMessage> : IInboundMessage<TMessage>
    {
        public IEndpoint Endpoint { get; set; }

        public MessageHeaderCollection Headers { get; set; } = new MessageHeaderCollection();

        public TMessage Message { get; set; }

        public int FailedAttempts { get; set; }

        object IInboundMessage.Message
        {
            get => Message;
            set => Message = (TMessage)value;
        }
    }
}
