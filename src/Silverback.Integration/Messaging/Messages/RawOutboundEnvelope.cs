// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawOutboundEnvelope" />
internal record RawOutboundEnvelope : RawBrokerEnvelope, IRawOutboundEnvelope
{
    public RawOutboundEnvelope(
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : this(null, headers, endpoint, producer, context, brokerMessageIdentifier)
    {
    }

    public RawOutboundEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(rawMessage, headers, endpoint)
    {
        Endpoint = Check.NotNull(endpoint, nameof(endpoint));
        Producer = Check.NotNull(producer, nameof(producer));
        Context = context;
        BrokerMessageIdentifier = brokerMessageIdentifier;
    }

    public new ProducerEndpoint Endpoint { get; }

    public IProducer Producer { get; }

    public SilverbackContext? Context { get; }

    public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }
}
