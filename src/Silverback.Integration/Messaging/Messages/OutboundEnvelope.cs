// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal record OutboundEnvelope : RawOutboundEnvelope, IOutboundEnvelope
{
    public OutboundEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        bool autoUnwrap = false,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(headers, endpoint, producer, brokerMessageIdentifier)
    {
        Message = message;

        if (Message is byte[] rawMessage)
            RawMessage = new MemoryStream(rawMessage);

        if (Message is Stream stream)
            RawMessage = stream;

        AutoUnwrap = autoUnwrap;
    }

    public bool AutoUnwrap { get; }

    public object? Message { get; set; }
}
