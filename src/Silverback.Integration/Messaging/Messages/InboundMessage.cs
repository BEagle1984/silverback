// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class InboundMessage : IInboundMessage
    {
        public InboundMessage()
        {
        }

        public InboundMessage(object message, IEnumerable<MessageHeader> headers, IOffset offset, IEndpoint endpoint, bool mustUnwrap)
        {
            Message = message;

            if (headers != null)
                Headers.AddRange(headers);

            Offset = offset;
            Endpoint = endpoint;
            MustUnwrap = mustUnwrap;
        }

        /// <inheritdoc />
        public object Message { get; }

        /// <inheritdoc />
        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        /// <inheritdoc />
        public IOffset Offset { get; }

        /// <inheritdoc />
        public IEndpoint Endpoint { get; }

        /// <inheritdoc />
        public bool MustUnwrap { get; }
    }

    internal class InboundMessage<TMessage> : InboundMessage, IInboundMessage<TMessage>
    {
        public InboundMessage(TMessage message, IEnumerable<MessageHeader> headers, IOffset offset, IEndpoint endpoint, bool mustUnwrap) 
            : base(message, headers, offset, endpoint, mustUnwrap)
        {
        }

        public InboundMessage(TMessage message, IRawInboundMessage rawMessage) : 
            base(message, rawMessage.Headers, rawMessage.Offset, rawMessage.Endpoint, rawMessage.MustUnwrap)
        {
        }

        public new TMessage Message => (TMessage)base.Message;
    }
}
