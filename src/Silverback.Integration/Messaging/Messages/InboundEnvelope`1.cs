// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope{TMessage}" />
internal sealed record InboundEnvelope<TMessage> : InboundEnvelope, IInboundEnvelope<TMessage>
    where TMessage : class
{
    public InboundEnvelope(IRawInboundEnvelope envelope, TMessage? message)
        : base(envelope, message)
    {
        Message = message;
    }

    public InboundEnvelope(
        TMessage? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(message, rawMessage, headers, endpoint, consumer, brokerMessageIdentifier)
    {
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);
}
