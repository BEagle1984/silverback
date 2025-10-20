// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal abstract record OutboundEnvelope<TMessage> : OutboundEnvelope, IOutboundEnvelope<TMessage>
    where TMessage : class
{
    protected OutboundEnvelope(TMessage? message, IProducer producer, ISilverbackContext? context = null)
        : base(message, producer, context)
    {
        Message = message;
    }

    protected OutboundEnvelope(IInboundEnvelope<TMessage> clonedEnvelope, IProducer producer, ISilverbackContext? context = null)
        : base(clonedEnvelope, producer, context)
    {
        Message = clonedEnvelope.Message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);
}
