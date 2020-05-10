// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    internal class OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
        where TMessage : class
    {
        public OutboundEnvelope(
            TMessage message,
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            Type? outboundConnectorType = null,
            bool autoUnwrap = false)
            : base(message, headers, endpoint, outboundConnectorType, autoUnwrap)
        {
        }

        public new TMessage Message => (TMessage)base.Message;
    }
}
