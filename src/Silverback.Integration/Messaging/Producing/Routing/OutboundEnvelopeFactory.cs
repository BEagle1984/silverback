// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     The factory used to build the <see cref="IOutboundEnvelope" /> or <see cref="IOutboundEnvelope{TMessage}" /> instances.
/// </summary>
public static class OutboundEnvelopeFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="IOutboundEnvelope" /> or <see cref="IOutboundEnvelope{TMessage}" />.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped in the envelope.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The destination endpoint.
    /// </param>
    /// <param name="producer">
    ///     The producer to be used to produce this message.
    /// </param>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    public static IOutboundEnvelope CreateEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context = null)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        return CreateEnvelope(message, headers, endpoint, producer, context, endpoint.Configuration.EnableSubscribing);
    }

    /// <summary>
    ///     Creates a new instance of <see cref="IOutboundEnvelope" /> or <see cref="IOutboundEnvelope{TMessage}" /> cloning the original
    ///     envelope and replacing the message.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped in the envelope.
    /// </param>
    /// <param name="originalEnvelope">
    ///     The original envelope to be cloned.
    /// </param>
    /// <returns>
    ///     The new <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    public static IOutboundEnvelope CreateEnvelope(object? message, IOutboundEnvelope originalEnvelope)
    {
        Check.NotNull(originalEnvelope, nameof(originalEnvelope));

        return CreateEnvelope(
            message,
            originalEnvelope.Headers,
            originalEnvelope.Endpoint,
            originalEnvelope.Producer,
            originalEnvelope.Context,
            originalEnvelope.AutoUnwrap);
    }

    private static IOutboundEnvelope CreateEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context,
        bool autoUnwrap)
    {
        Check.NotNull(endpoint, nameof(endpoint));
        Check.NotNull(producer, nameof(producer));

        if (message is IMessageWithHeaders { Headers.Count: > 0 } messageWithHeaders)
        {
            IOutboundEnvelope outboundEnvelope = messageWithHeaders.Message == null
                ? new OutboundEnvelope(null, headers, endpoint, producer, context, autoUnwrap)
                : (IOutboundEnvelope)Activator.CreateInstance(
                    typeof(OutboundEnvelope<>).MakeGenericType(messageWithHeaders.Message.GetType()),
                    messageWithHeaders.Message,
                    headers,
                    endpoint,
                    producer,
                    context,
                    autoUnwrap)!;

            outboundEnvelope.Headers.AddRange(messageWithHeaders.Headers);

            return outboundEnvelope;
        }

        return message == null
            ? new OutboundEnvelope(null, headers, endpoint, producer, context, autoUnwrap)
            : (IOutboundEnvelope)Activator.CreateInstance(
                typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                message,
                headers,
                endpoint,
                producer,
                context,
                autoUnwrap)!;
    }
}
