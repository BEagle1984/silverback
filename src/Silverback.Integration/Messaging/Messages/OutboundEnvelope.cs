// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        SilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(headers, endpoint, producer, context, brokerMessageIdentifier)
    {
        Message = message;

        if (message is byte[] rawMessage)
            RawMessage = new MemoryStream(rawMessage);

        if (message is Stream stream)
            RawMessage = stream;
    }

    public object? Message { get; init; }

    public virtual Type MessageType => Message?.GetType() ?? typeof(object);

    public IOutboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage) => this with
    {
        RawMessage = newRawMessage,
        Headers = new MessageHeaderCollection(Headers)
    };
}
