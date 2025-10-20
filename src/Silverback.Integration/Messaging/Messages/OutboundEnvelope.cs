// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal abstract record OutboundEnvelope : BrokerEnvelope, IInternalOutboundEnvelope
{
    private ProducerEndpoint? _endpoint;

    protected OutboundEnvelope(object? message, IProducer producer, ISilverbackContext? context = null)
    {
        Producer = Check.NotNull(producer, nameof(producer));
        Context = context;
        Message = message;

        if (message is byte[] rawMessage)
            RawMessage = new MemoryStream(rawMessage);

        if (message is Stream stream)
            RawMessage = stream;
    }

    protected OutboundEnvelope(IInboundEnvelope clonedEnvelope, IProducer producer, ISilverbackContext? context = null)
        : base(clonedEnvelope)
    {
        Producer = Check.NotNull(producer, nameof(producer));
        Context = context;
        Message = clonedEnvelope.Message;
    }

    public IProducer Producer { get; }

    public ProducerEndpointConfiguration EndpointConfiguration => Producer.EndpointConfiguration;

    public ISilverbackContext? Context { get; }

    public string? ResolvedEndpoint { get; private set; }

    public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }

    public ProducerEndpoint GetEndpoint() => _endpoint ??= EndpointConfiguration.EndpointResolver.GetEndpoint(this);

    public IOutboundEnvelope SetRawMessage(Stream? rawMessage)
    {
        RawMessage = rawMessage;
        return this;
    }

    public IOutboundEnvelope AddHeader(string name, object? value)
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

    public IOutboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage) => this with
    {
        RawMessage = newRawMessage,
        Headers = new MessageHeaderCollection(Headers)
    };

    public IOutboundEnvelope SetBrokerMessageIdentifier(IBrokerMessageIdentifier? brokerMessageIdentifier)
    {
        BrokerMessageIdentifier = brokerMessageIdentifier;
        return this;
    }

    public IInternalOutboundEnvelope SetResolvedEndpoint(string? resolvedEndpoint)
    {
        ResolvedEndpoint = resolvedEndpoint;
        return this;
    }
}
