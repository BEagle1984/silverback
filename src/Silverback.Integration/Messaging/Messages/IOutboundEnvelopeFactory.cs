// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IOutboundEnvelope{TMessage}" />.
/// </summary>
public interface IOutboundEnvelopeFactory
{
    /// <summary>
    ///     Creates the <see cref="IOutboundEnvelope" />.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="context">
    ///     The optional <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    IOutboundEnvelope Create(object? message, ISilverbackContext? context = null);

    /// <summary>
    ///     Creates the <see cref="IOutboundEnvelope" /> from the specified <see cref="IInboundEnvelope" />.
    /// </summary>
    /// <param name="envelope">
    ///     The inbound envelope to be cloned.
    /// </param>
    /// <param name="context">
    ///     The optional <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    IOutboundEnvelope CreateFromInboundEnvelope(IInboundEnvelope envelope, ISilverbackContext? context = null);

    /// <summary>
    ///     Creates the <see cref="IOutboundEnvelope{TMessage}" />.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the wrapped message.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="context">
    ///     The optional <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </returns>
    IOutboundEnvelope<TMessage> Create<TMessage>(TMessage? message, ISilverbackContext? context = null)
        where TMessage : class;

    /// <summary>
    ///     Clones the <see cref="IOutboundEnvelope" /> replacing the message with the specified one. The new <see cref="IOutboundEnvelope" />
    ///     will be created according to the new message type.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="messageType">
    ///     The type of the message.
    /// </param>
    /// <param name="envelope">
    ///     The envelope to copy.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    IOutboundEnvelope CloneReplacingMessage(object? message, Type messageType, IOutboundEnvelope envelope);

    /// <summary>
    ///     Clones the <see cref="IOutboundEnvelope" /> replacing the message with the specified one. The new <see cref="IOutboundEnvelope" />
    ///     will be created according to the new message type.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the wrapped message.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="envelope">
    ///     The envelope to copy.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    IOutboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IOutboundEnvelope envelope)
        where TMessage : class;
}
