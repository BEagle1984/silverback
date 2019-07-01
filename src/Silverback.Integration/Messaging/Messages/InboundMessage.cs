// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class InboundMessage : IInboundMessage
    {
         public InboundMessage(byte[] rawContent, IEnumerable<MessageHeader> headers, IOffset offset, IEndpoint endpoint, bool mustUnwrap)
        {
            if (headers != null)
                Headers.AddRange(headers);

            Offset = offset;
            Endpoint = endpoint;
            RawContent = rawContent;
            MustUnwrap = mustUnwrap;
        }

        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        public IOffset Offset { get; }

        public IEndpoint Endpoint { get; }

        public byte[] RawContent { get; }

        public object Content { get; set; }

        public bool MustUnwrap { get; }
    }

    internal class InboundMessage<TContent> : InboundMessage, IInboundMessage<TContent>
    {
        public InboundMessage(byte[] rawContent, IEnumerable<MessageHeader> headers, IOffset offset, IEndpoint endpoint, bool mustUnwrap)
            : base(rawContent, headers, offset, endpoint, mustUnwrap)
        {
        }

        public InboundMessage(IInboundMessage message)
            : base(message.RawContent, message.Headers, message.Offset, message.Endpoint, message.MustUnwrap)
        {
            if (message.Headers != null)
                Headers.AddRange(message.Headers);

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
