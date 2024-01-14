// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IRawOutboundEnvelope" />
public interface IOutboundEnvelope : IBrokerEnvelope, IRawOutboundEnvelope
{
    /// <summary>
    ///     Clones the envelope and replaces the message with the specified one.
    /// </summary>
    /// <param name="newMessage">
    ///     The new message to be set.
    /// </param>
    /// <returns>
    ///     The new envelope.
    /// </returns>
    public IOutboundEnvelope CloneReplacingMessage(object? newMessage);

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
}
