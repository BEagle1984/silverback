// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class ProcessedOutboundEnvelope : OutboundEnvelope
    {
        public ProcessedOutboundEnvelope(
            object? message,
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            bool autoUnwrap = false,
            IOffset? offset = null)
            : base(message, headers, endpoint, autoUnwrap, offset)
        {
        }
    }
}
