// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IInboundEnvelope{TMessage}" />.
/// </summary>
public interface IInboundEnvelopeFactory
{
    /// <summary>
    ///     Creates the <see cref="IInboundEnvelope" />.
    /// </summary>
    /// <param name="rawMessage">
    ///     The serialized message.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint from which the message was consumed.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The message identifier on the message broker (the Kafka offset or similar).
    /// </param>
    /// <returns>
    ///     The <see cref="IInboundEnvelope" /> instance.
    /// </returns>
    IInboundEnvelope Create(Stream? rawMessage, ConsumerEndpoint endpoint, IBrokerMessageIdentifier brokerMessageIdentifier);

    /// <summary>
    ///     Creates the <see cref="IInboundEnvelope{TMessage}" />.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the wrapped message.
    /// </typeparam>
    /// <param name="rawMessage">
    ///     The serialized message.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint from which the message was consumed.
    /// </param>
    /// <param name="brokerMessageIdentifier">
    ///     The message identifier on the message broker (the Kafka offset or similar).
    /// </param>
    /// <returns>
    ///     The <see cref="IInboundEnvelope{TMessage}" /> instance.
    /// </returns>
    IInboundEnvelope<TMessage> Create<TMessage>(Stream? rawMessage, ConsumerEndpoint endpoint, IBrokerMessageIdentifier brokerMessageIdentifier)
        where TMessage : class;

    /// <summary>
    ///     Clones the <see cref="IInboundEnvelope" /> replacing the message with the specified one. The new <see cref="IInboundEnvelope" />
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
    ///     The <see cref="IInboundEnvelope" /> instance.
    /// </returns>
    IInboundEnvelope CloneReplacingMessage(object? message, Type messageType, IInboundEnvelope envelope);

    /// <summary>
    ///     Clones the <see cref="IInboundEnvelope" /> replacing the message with the specified one. The new <see cref="IInboundEnvelope" />
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
    ///     The <see cref="IInboundEnvelope" /> instance.
    /// </returns>
    IInboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IInboundEnvelope envelope)
        where TMessage : class;
}
