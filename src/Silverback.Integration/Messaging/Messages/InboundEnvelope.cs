// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope" />
internal abstract record InboundEnvelope : BrokerEnvelope, IInboundEnvelope
{
    protected InboundEnvelope(
        object? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(rawMessage, headers)
    {
        Message = message;
        Endpoint = Check.NotNull(endpoint, nameof(endpoint));
        BrokerMessageIdentifier = Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));
        Consumer = Check.NotNull(consumer, nameof(consumer));
    }

    protected InboundEnvelope(object? message, IInboundEnvelope clonedEnvelope)
        : base(clonedEnvelope)
    {
        Message = message;
        Endpoint = clonedEnvelope.Endpoint;
        BrokerMessageIdentifier = clonedEnvelope.BrokerMessageIdentifier;
        Consumer = clonedEnvelope.Consumer;
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
