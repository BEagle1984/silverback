// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>SetMessageId</c> methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     Uses the specified value provider function to set the message id for each produced message.
    ///     Depending on the broker the provided value might be forwarded as header or similar (or used as Kafka key for the produced message).
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder SetMessageId(Func<TMessage?, object?> valueProvider)
    {
        Check.NotNull(valueProvider, nameof(valueProvider));
        return AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessage>(valueProvider));
    }

    /// <summary>
    ///     Uses the specified value provider function to set the message id for each produced message of the specified child type.
    ///     Depending on the broker the provided value might be forwarded as header or similar (or used as Kafka key for the produced message).
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
    public TBuilder SetMessageId<TMessageChildType>(Func<TMessageChildType?, object?> valueProvider)
        where TMessageChildType : TMessage
    {
        Check.NotNull(valueProvider, nameof(valueProvider));
        return AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessageChildType>(valueProvider));
    }

    /// <summary>
    ///     Uses the specified value provider function to set the message id for each produced message.
    ///     Depending on the broker the provided value might be forwarded as header or similar (or used as Kafka key for the produced message).
    /// </summary>
    /// <param name="valueProvider">
    ///     The value provider function.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder SetMessageId(Func<IOutboundEnvelope<TMessage>, object?> valueProvider)
    {
        AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessage>(valueProvider));
        return This;
    }

    /// <summary>
    ///     Uses the specified value provider function to set the message id for each produced message of the specified child type.
    ///     Depending on the broker the provided value might be forwarded as header or similar (or used as Kafka key for the produced message).
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
    public TBuilder SetMessageId<TMessageChildType>(Func<IOutboundEnvelope<TMessageChildType>, object?> valueProvider)
        where TMessageChildType : TMessage
    {
        AddMessageEnricher(new OutboundMessageIdHeadersEnricher<TMessageChildType>(valueProvider));
        return This;
    }
}
