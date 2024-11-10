// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawOutboundEnvelope" />
public interface IOutboundEnvelope : IBrokerEnvelope, IRawOutboundEnvelope
{
    /// <summary>
    ///     Gets the destination endpoint for the specific message.
    /// </summary>
    /// <returns>
    ///     The endpoint.
    /// </returns>
    ProducerEndpoint GetEndpoint();

    /// <summary>
    ///     Clones the envelope and replaces the raw message with the specified one.
    /// </summary>
    /// <param name="newRawMessage">
    ///     The new raw message to be set.
    /// </param>
    /// <returns>
    ///     The new envelope.
    /// </returns>
    public IOutboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage);

    /// <summary>
    ///     Clones the envelope and replaces the contained message with the specified one.
    /// </summary>
    /// <remarks>
    ///     The raw message will be cleared.
    /// </remarks>
    /// <typeparam name="TNewMessage">
    ///     The type of the new message.
    /// </typeparam>
    /// <param name="newMessage">
    ///     The new message to be set.
    /// </param>
    /// <returns>
    ///     The new envelope.
    /// </returns>
    public IOutboundEnvelope<TNewMessage> CloneReplacingMessage<TNewMessage>(TNewMessage newMessage)
        where TNewMessage : class;
}
