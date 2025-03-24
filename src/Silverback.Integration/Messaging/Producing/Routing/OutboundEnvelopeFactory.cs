// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
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
    /// <param name="endpointConfiguration">
    ///     The destination endpoint configuration.
    /// </param>
    /// <param name="producer">
    ///     The producer to be used to produce this message.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    public static IOutboundEnvelope CreateEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null)
    {
        Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));
        Check.NotNull(producer, nameof(producer));

        return message == null
            ? new OutboundEnvelope(null, headers, endpointConfiguration, producer, context)
            : (IOutboundEnvelope)Activator.CreateInstance(
                typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                message,
                headers,
                endpointConfiguration,
                producer,
                context)!;
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
            originalEnvelope.EndpointConfiguration,
            originalEnvelope.Producer,
            originalEnvelope.Context);
    }
}
