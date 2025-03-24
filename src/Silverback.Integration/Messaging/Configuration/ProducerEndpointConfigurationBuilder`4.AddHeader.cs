// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>AddHeader</c> methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     Adds the specified header to all produced messages.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader(string name, object? value)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        return AddMessageEnricher(new StaticOutboundHeadersEnricher(name, value));
    }

    /// <summary>
    ///     Adds the specified header to all produced messages of the specified child type.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader<TMessageChildType>(string name, object? value)
        where TMessageChildType : TMessage
    {
        Check.NotNullOrEmpty(name, nameof(name));

        return AddMessageEnricher(new GenericOutboundHeadersEnricher<TMessageChildType>(name, value));
    }

    /// <summary>
    ///     Adds the specified header to all produced messages, using a value provider function to determine the header value for each message.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader(string name, Func<TMessage?, object?> valueProvider)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(valueProvider, nameof(valueProvider));

        return AddMessageEnricher(new GenericOutboundHeadersEnricher<TMessage>(name, valueProvider));
    }

    /// <summary>
    ///     Adds the specified header to all produced messages of the specified child type, using a value provider function to determine the
    ///     header value for each message.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader<TMessageChildType>(string name, Func<TMessageChildType?, object?> valueProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(valueProvider, nameof(valueProvider));

        return AddMessageEnricher(new GenericOutboundHeadersEnricher<TMessageChildType>(name, valueProvider));
    }

    /// <summary>
    ///     Adds the specified header to all produced messages, using a value provider function to determine the header value for each message.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader(string name, Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(valueProvider, nameof(valueProvider));

        return AddMessageEnricher(new GenericOutboundHeadersEnricher<TMessage>(name, valueProvider));
    }

    /// <summary>
    ///     Adds the specified header to all produced messages of the specified child type, using a value provider function to determine the
    ///     header value for each message.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader<TMessageChildType>(string name, Func<IOutboundEnvelope<TMessageChildType>, object?> valueProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(valueProvider, nameof(valueProvider));

        return AddMessageEnricher(new GenericOutboundHeadersEnricher<TMessageChildType>(name, valueProvider));
    }
}
