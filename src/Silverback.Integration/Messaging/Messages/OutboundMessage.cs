// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Messages
{
    internal class OutboundMessage : RawOutboundMessage, IOutboundMessageInternal
    {
        internal OutboundMessage(object content, IEnumerable<MessageHeader> headers, IOutboundRoute route)
            : this(content, headers, route?.DestinationEndpoint)
        {
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public OutboundMessage(object content, IEnumerable<MessageHeader> headers, IProducerEndpoint endpoint, IOffset offset = null)
            : base(content, headers, endpoint, offset)
        {
            Content = content;
        }

        public object Content { get; }

        public IOutboundRoute Route { get; }
    }

    internal class OutboundMessage<TContent> : OutboundMessage, IOutboundMessage<TContent>
    {
        public OutboundMessage(TContent content, IEnumerable<MessageHeader> headers, IOutboundRoute route)
            : base(content, headers, route)
        {
        }

        public OutboundMessage(TContent content, IEnumerable<MessageHeader> headers, IProducerEndpoint endpoint)
            : base(content, headers, endpoint)
        {
        }

        public new TContent Content => (TContent)base.Content;
    }
}