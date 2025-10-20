// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope{TMessage}" />
internal abstract record InboundEnvelope<TMessage> : InboundEnvelope, IInboundEnvelope<TMessage>
    where TMessage : class
{
    protected InboundEnvelope(
        TMessage? message,
        Stream? rawMessage,
         ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(message, rawMessage, endpoint, consumer, brokerMessageIdentifier)
    {
        Message = message;
    }

    protected InboundEnvelope(TMessage? message, IInboundEnvelope clonedEnvelope)
        : base(message, clonedEnvelope)
    {
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);
}
