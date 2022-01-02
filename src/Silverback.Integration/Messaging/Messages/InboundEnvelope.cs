// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawInboundEnvelope" />
internal class InboundEnvelope : RawInboundEnvelope, IInboundEnvelope
{
    public InboundEnvelope(IRawInboundEnvelope envelope)
        : this(
            envelope.RawMessage,
            envelope.Headers,
            envelope.BrokerMessageIdentifier,
            envelope.Endpoint)
    {
    }

    public InboundEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier brokerMessageIdentifier,
        ConsumerEndpoint endpoint)
        : base(
            rawMessage,
            headers,
            endpoint,
            brokerMessageIdentifier)
    {
    }

    public InboundEnvelope(
        object message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier brokerMessageIdentifier,
        ConsumerEndpoint endpoint)
        : base(
            rawMessage,
            headers,
            endpoint,
            brokerMessageIdentifier)
    {
        Message = message;
    }

    public bool AutoUnwrap => true;

    public object? Message { get; protected set; }
}
