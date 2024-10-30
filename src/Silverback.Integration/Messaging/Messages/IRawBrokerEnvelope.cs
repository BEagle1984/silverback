// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the serialized inbound or outbound message.
/// </summary>
public interface IRawBrokerEnvelope
{
    /// <summary>
    ///     Gets the optional message headers.
    /// </summary>
    MessageHeaderCollection Headers { get; }

    /// <summary>
    ///     Gets the source or destination endpoint.
    /// </summary>
    Endpoint Endpoint { get; }

    /// <summary>
    ///     Gets or sets the serialized message body.
    /// </summary>
    Stream? RawMessage { get; set; }

    /// <summary>
    ///     Gets the message id header (<see cref="DefaultMessageHeaders.MessageId" />) value.
    /// </summary>
    /// <returns>
    ///     The message id or <c>null</c> if not found.
    /// </returns>
    string? GetMessageId();
}
