// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawInboundEnvelope" />
internal class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
{
    [SuppressMessage("", "CA2000", Justification = "MemoryStream doesn't actually need disposing")]
    public RawInboundEnvelope(
        byte[]? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : this(
            rawMessage != null ? new MemoryStream(rawMessage) : null,
            headers,
            endpoint,
            brokerMessageIdentifier)
    {
    }

    public RawInboundEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        : base(rawMessage, headers, endpoint)
    {
        Endpoint = Check.NotNull(endpoint, nameof(endpoint));
        BrokerMessageIdentifier = Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));
    }

    public new ConsumerEndpoint Endpoint { get; }

    public IBrokerMessageIdentifier BrokerMessageIdentifier { get; }
}
