// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawOutboundEnvelope" />
internal record RawOutboundEnvelope : RawBrokerEnvelope, IRawOutboundEnvelope
{
    public RawOutboundEnvelope(
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : this(null, headers, endpointConfiguration, producer, context, brokerMessageIdentifier)
    {
    }

    public RawOutboundEnvelope(
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(rawMessage, headers)
    {
        EndpointConfiguration = Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));
        Producer = Check.NotNull(producer, nameof(producer));
        Context = context;
        BrokerMessageIdentifier = brokerMessageIdentifier;
    }

    public ProducerEndpointConfiguration EndpointConfiguration { get; }

    public IProducer Producer { get; }

    public ISilverbackContext? Context { get; }

    public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }

    /// <summary>
    ///     Adds a new header.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public IRawOutboundEnvelope AddHeader(string name, object value)
    {
        Headers.Add(name, value);
        return this;
    }

    /// <summary>
    ///     Adds a new header or replaces the header with the same name.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public IRawOutboundEnvelope AddOrReplaceHeader(string name, object? newValue)
    {
        Headers.AddOrReplace(name, newValue);
        return this;
    }

    /// <summary>
    ///     Adds a new header if no header with the same name is already set.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public IRawOutboundEnvelope AddHeaderIfNotExists(string name, object? newValue)
    {
        Headers.AddIfNotExists(name, newValue);
        return this;
    }

    /// <summary>
    ///     Sets the message id header (<see cref="DefaultMessageHeaders.MessageId" />.
    /// </summary>
    /// <param name="value">
    ///     The message id.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public IRawOutboundEnvelope SetMessageId(object? value)
    {
        Headers.AddOrReplace(DefaultMessageHeaders.MessageId, value);
        return this;
    }
}
