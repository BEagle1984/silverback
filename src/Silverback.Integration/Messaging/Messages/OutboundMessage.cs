// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Messages
{
    internal class OutboundMessage : IOutboundMessageInternal
    {
        internal OutboundMessage(object content, IEnumerable<MessageHeader> headers, IOutboundRoute route)
            : this(content, headers, route?.DestinationEndpoint)
        {
            Route = route;
        }

        public OutboundMessage(object content, IEnumerable<MessageHeader> headers, IEndpoint endpoint, IOffset offset)
            : this(content, headers, endpoint)
        {
            Offset = offset;
        }

        public OutboundMessage(object content, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
        {
            if (headers != null)
                Headers.AddRange(headers);

            Content = content;
            Endpoint = endpoint;
        }

        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();

        public IOffset Offset { get; set; }

        public IEndpoint Endpoint { get; }

        public object Content { get; }

        public byte[] RawContent { get; set; }

        public IOutboundRoute Route { get; }
    }

    internal class OutboundMessage<TContent> : OutboundMessage, IOutboundMessage<TContent>
    {
        public OutboundMessage(TContent content, IEnumerable<MessageHeader> headers, IOutboundRoute route)
            : base(content, headers, route)
        {
        }

        public OutboundMessage(TContent content, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
            : base(content, headers, endpoint)
        {
        }

        public new TContent Content => (TContent)base.Content;
    }
}