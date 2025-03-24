// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal sealed record OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    where TMessage : class
{
    public OutboundEnvelope(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null)
        : base(message, headers, endpointConfiguration, producer, context)
    {
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);
}
