// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope" />
internal record InboundEnvelope : BrokerEnvelope, IInboundEnvelope
{
    public InboundEnvelope(IInboundEnvelope envelope)
        : this(
            envelope.RawMessage,
            envelope.Headers,
            envelope.Endpoint,
            envelope.Consumer,
            envelope.BrokerMessageIdentifier)
    {
    }

    public InboundEnvelope(IInboundEnvelope envelope, object? message)
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
        object? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : this(rawMessage, headers, endpoint, consumer, brokerMessageIdentifier)
    {
        Message = message;
    }

    public InboundEnvelope(
        byte[]? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : this(
            rawMessage != null ? new MemoryStream(rawMessage) : null,
            headers,
            endpoint,
            consumer,
            brokerMessageIdentifier)
    {
    }

    public InboundEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(rawMessage, headers)
    {
        Endpoint = Check.NotNull(endpoint, nameof(endpoint));
        BrokerMessageIdentifier = Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));
        Consumer = Check.NotNull(consumer, nameof(consumer));
    }

    public ConsumerEndpoint Endpoint { get; }

    public IConsumer Consumer { get; }

    public IBrokerMessageIdentifier BrokerMessageIdentifier { get; }

    public IInboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage) => this with
    {
        RawMessage = newRawMessage,
        Headers = new MessageHeaderCollection(Headers)
    };
}
