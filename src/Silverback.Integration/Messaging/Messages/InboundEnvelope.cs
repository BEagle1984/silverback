// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawInboundEnvelope" />
internal record InboundEnvelope : RawInboundEnvelope, IInboundEnvelope
{
    public InboundEnvelope(IRawInboundEnvelope envelope)
        : this(
            envelope.RawMessage,
            envelope.Headers,
            envelope.Endpoint,
            envelope.Consumer,
            envelope.BrokerMessageIdentifier)
    {
    }

    public InboundEnvelope(IRawInboundEnvelope envelope, object? message)
        : this(
            message,
            envelope.RawMessage,
            envelope.Headers,
            envelope.Endpoint,
            envelope.Consumer,
            envelope.BrokerMessageIdentifier)
    {
    }

    public InboundEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(rawMessage, headers, endpoint, consumer, brokerMessageIdentifier)
    {
    }

    public InboundEnvelope(
        object? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(rawMessage, headers, endpoint, consumer, brokerMessageIdentifier)
    {
        Message = message;
    }

    public virtual Type MessageType => Message?.GetType() ?? typeof(object);

    public bool AutoUnwrap => true;

    public object? Message { get; set; }

    public bool IsTombstone => Message is null or ITombstone;
}
