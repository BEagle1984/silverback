// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IOutboundEnvelopeInternal" />
    internal class OutboundEnvelope : RawOutboundEnvelope, IOutboundEnvelopeInternal
    {
        public OutboundEnvelope(
            object message,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            Type outboundConnectorType = null,
            bool autoUnwrap = false,
            IOffset offset = null)
            : base(headers, endpoint, offset)
        {
            Message = message;
            OutboundConnectorType = outboundConnectorType;
            AutoUnwrap = autoUnwrap;
        }

        public object Message { get; }

        public bool AutoUnwrap { get; }

        public Type OutboundConnectorType { get; }
    }

    /// <inheritdoc cref="IOutboundEnvelope{TMessage}" />
    internal class OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    {
        public OutboundEnvelope(
            TMessage message,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            Type outboundConnectorType = null,
            bool autoUnwrap = false)
            : base(message, headers, endpoint, outboundConnectorType, autoUnwrap)
        {
        }

        public new TMessage Message => (TMessage) base.Message;
    }
}