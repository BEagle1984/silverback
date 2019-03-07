using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Messaging.Messages
{
    public class InboundMessage<TMessage> : IInboundMessage<TMessage>
    {
        public IEndpoint Endpoint { get; set; }

        public TMessage Message { get; set; }

        object IInboundMessage.Message
        {
            get => Message;
            set => Message = (TMessage)value;
        }
    }
}
