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
        internal OutboundEnvelope(
            object message,
            IEnumerable<MessageHeader> headers,
            IOutboundRoute route,
            bool autoUnwrap = false)
            : this(message, headers, route?.DestinationEndpoint, autoUnwrap)
        {
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public OutboundEnvelope(
            object message,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            bool autoUnwrap = false,
            IOffset offset = null)
            : base(headers, endpoint, offset)
        {
            Message = message;
            AutoUnwrap = autoUnwrap;
        }

        public object Message { get; }

        public bool AutoUnwrap { get; }

        public IOutboundRoute Route { get; }
    }

    internal class OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    {
        public OutboundEnvelope(
            TMessage message,
            IEnumerable<MessageHeader> headers,
            IOutboundRoute route,
            bool autoUnwrap = false)
            : base(message, headers, route, autoUnwrap)
        {
        }

        public OutboundEnvelope(
            TMessage message,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            bool autoUnwrap = false)
            : base(message, headers, endpoint, autoUnwrap)
        {
        }

        public new TMessage Message => (TMessage) base.Message;
    }
}