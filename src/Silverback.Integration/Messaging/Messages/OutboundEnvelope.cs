// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Messages
{
    internal class OutboundEnvelope : RawOutboundEnvelope, IOutboundEnvelopeInternal
    {
        internal OutboundEnvelope(object message, IEnumerable<MessageHeader> headers, IOutboundRoute route)
            : this(message, headers, route?.DestinationEndpoint)
        {
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public OutboundEnvelope(
            object message,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            IOffset offset = null)
            : base(message, headers, endpoint, offset)
        {
            Message = message;
        }

        public object Message { get; }

        public IOutboundRoute Route { get; }
    }

    internal class OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    {
        public OutboundEnvelope(TMessage message, IEnumerable<MessageHeader> headers, IOutboundRoute route)
            : base(message, headers, route)
        {
        }

        public OutboundEnvelope(TMessage message, IEnumerable<MessageHeader> headers, IProducerEndpoint endpoint)
            : base(message, headers, endpoint)
        {
        }

        public new TMessage Message => (TMessage) base.Message;
    }
}