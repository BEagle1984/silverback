// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages;

internal sealed class OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    where TMessage : class
{
    public OutboundEnvelope(
        TMessage message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        bool autoUnwrap = false)
        : base(message, headers, endpoint, autoUnwrap)
    {
        Message = message;
    }

    public new TMessage? Message { get; }
}
