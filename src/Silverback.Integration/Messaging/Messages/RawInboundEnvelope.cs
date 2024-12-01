// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawInboundEnvelope" />
internal record RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
{
    public RawInboundEnvelope(
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

    public RawInboundEnvelope(
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

    public IRawInboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage) => this with
    {
        RawMessage = newRawMessage,
        Headers = new MessageHeaderCollection(Headers)
    };
}
