// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal record OutboundEnvelope : RawOutboundEnvelope, IOutboundEnvelope
{
    private ProducerEndpoint? _endpoint;

    public OutboundEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(headers, endpointConfiguration, producer, context, brokerMessageIdentifier)
    {
        Message = message;

        if (message is byte[] rawMessage)
            RawMessage = new MemoryStream(rawMessage);

        if (message is Stream stream)
            RawMessage = stream;
    }

    public object? Message { get; init; }

    public virtual Type MessageType => Message?.GetType() ?? typeof(object);

    public bool IsTombstone => Message is null or ITombstone;

    public ProducerEndpoint GetEndpoint() => _endpoint ??= EndpointConfiguration.EndpointResolver.GetEndpoint(this);

    public IOutboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage) => this with
    {
        RawMessage = newRawMessage,
        Headers = new MessageHeaderCollection(Headers)
    };

    public IOutboundEnvelope<TNewMessage> CloneReplacingMessage<TNewMessage>(TNewMessage newMessage)
        where TNewMessage : class =>
        new OutboundEnvelope<TNewMessage>(
            newMessage,
            Headers,
            EndpointConfiguration,
            Producer,
            Context);
}
