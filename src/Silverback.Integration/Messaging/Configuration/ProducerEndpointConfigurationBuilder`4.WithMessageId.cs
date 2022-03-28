// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>WithMessageId</c> methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     Uses the specified value provider function to set the message id header for each produced message.
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithMessageId(Func<TMessage?, object?> valueProvider)
    {
        Check.NotNull(valueProvider, nameof(valueProvider));
        return AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessage>(valueProvider));
    }

    /// <summary>
    ///     Uses the specified value provider function to set the message id header for each produced message of the specified child type.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithMessageId<TMessageChildType>(Func<TMessageChildType?, object?> valueProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(valueProvider, nameof(valueProvider));
        return AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessageChildType>(valueProvider));
    }

    /// <summary>
    ///     Uses the specified value provider function to set the message id header for each produced message.
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithMessageId(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
    {
        AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessage>(valueProvider));
        return This;
    }

    /// <summary>
    ///     Uses the specified value provider function to set the message id header for each produced message of the specified child type.
    /// </summary>
    /// <typeparam name="TMessageChildType">
    ///     The type of the messages to be enriched with this header.
    /// </typeparam>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithMessageId<TMessageChildType>(Func<IOutboundEnvelope<TMessageChildType>, object?> valueProvider)
        where TMessageChildType : TMessage
    {
        AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessageChildType>(valueProvider));
        return This;
    }
}
