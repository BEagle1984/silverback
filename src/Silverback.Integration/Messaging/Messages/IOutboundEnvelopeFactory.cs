// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IOutboundEnvelope" /> and <see cref="IOutboundEnvelope{TMessage}" />.
/// </summary>
public interface IOutboundEnvelopeFactory
{
    /// <summary>
    ///     Creates the <see cref="IOutboundEnvelope" />.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="endpointConfiguration">
    ///     The destination endpoint configuration.
    /// </param>
    /// <param name="context">
    ///     The optional <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    IOutboundEnvelope Create(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null);

    /// <summary>
    ///     Creates the <see cref="IOutboundEnvelope{TMessage}" />.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the wrapped message.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="endpointConfiguration">
    ///     The destination endpoint configuration.
    /// </param>
    /// <param name="context">
    ///     The optional <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </returns>
    IOutboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null)
        where TMessage : class;
}
