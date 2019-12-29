// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class InboundMessage : RawInboundMessage, IInboundMessage
    {
        public InboundMessage(RawBrokerMessage message, bool mustUnwrap)
            : this(message.RawContent, message.Headers, message.Offset, (IConsumerEndpoint)message.Endpoint, mustUnwrap)
        {
        }

        public InboundMessage(byte[] rawContent, IEnumerable<MessageHeader> headers, IOffset offset, IConsumerEndpoint endpoint, bool mustUnwrap) 
            : base(rawContent, headers, endpoint, offset)
        {
            MustUnwrap = mustUnwrap;
        }

        public object Content { get; set; }

        public bool MustUnwrap { get; }
    }

    internal class InboundMessage<TContent> : InboundMessage, IInboundMessage<TContent>
    {
        public InboundMessage(byte[] rawContent, IEnumerable<MessageHeader> headers, IOffset offset, IConsumerEndpoint endpoint, bool mustUnwrap)
            : base(rawContent, headers, offset, endpoint, mustUnwrap)
        {
        }

        public InboundMessage(IInboundMessage message)
            : base(message.RawContent, message.Headers, message.Offset, message.Endpoint, message.MustUnwrap)
        {
            if (message.Content != null)
                Content = (TContent)message.Content;
        }

        public new TContent Content
        {
            get => (TContent)base.Content;
            set => base.Content = value;
        }
    }
}
