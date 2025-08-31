// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal abstract record OutboundEnvelope : BrokerEnvelope, IOutboundEnvelope
{
    private ProducerEndpoint? _endpoint;

    public OutboundEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(null, headers)
    {
        EndpointConfiguration = Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));
        Producer = Check.NotNull(producer, nameof(producer));
        Context = context;
        BrokerMessageIdentifier = brokerMessageIdentifier;
        Message = message;

        if (message is byte[] rawMessage)
            RawMessage = new MemoryStream(rawMessage);

        if (message is Stream stream)
            RawMessage = stream;
    }

    public ProducerEndpointConfiguration EndpointConfiguration { get; }

    public IProducer Producer { get; }

    public ISilverbackContext? Context { get; }

    public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }

    public IOutboundEnvelope AddHeader(string name, object value)
    {
        Headers.Add(name, value);
        return this;
    }

    public IOutboundEnvelope AddOrReplaceHeader(string name, object? newValue)
    {
        Headers.AddOrReplace(name, newValue);
        return this;
    }

    public IOutboundEnvelope AddHeaderIfNotExists(string name, object? newValue)
    {
        Headers.AddIfNotExists(name, newValue);
        return this;
    }

    public ProducerEndpoint GetEndpoint() => _endpoint ??= EndpointConfiguration.EndpointResolver.GetEndpoint(this);

    public IOutboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage) => this with
    {
        RawMessage = newRawMessage,
        Headers = new MessageHeaderCollection(Headers)
    };
}
