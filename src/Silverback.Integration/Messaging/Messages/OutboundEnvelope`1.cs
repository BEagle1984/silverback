// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal sealed record OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    where TMessage : class
{
    public OutboundEnvelope(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context = null,
        bool autoUnwrap = false)
        : base(message, headers, endpoint, producer, context, autoUnwrap)
    {
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);
}
