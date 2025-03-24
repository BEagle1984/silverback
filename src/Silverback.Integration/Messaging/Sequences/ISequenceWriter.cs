// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     Can set the headers used to match the message with the sequence it belongs to. If needed it can
///     also
///     split a single message into multiple messages.
/// </summary>
public interface ISequenceWriter
{
    /// <summary>
    ///     Checks whether this writer can and must handle the specified message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be handled.
    /// </param>
    /// <returns>
    ///     A value indicating whether this writer can and must handle the message.
    /// </returns>
    bool CanHandle(IOutboundEnvelope envelope);

    /// <summary>
    ///     Sets the headers used to match the message with the sequence it belongs to. If needed it can
    ///     also split a single message into multiple messages.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be handled.
    /// </param>
    /// <returns>
    ///     An <see cref="IAsyncEnumerable{T}" /> with the envelopes containing the messages to be
    ///     produced.
    /// </returns>
    IAsyncEnumerable<IOutboundEnvelope> ProcessMessageAsync(IOutboundEnvelope envelope);
}
