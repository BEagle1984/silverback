// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Encapsulates the information to be stored in the outbox to be able to replay the message at a later time.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxMessage" /> class.
    /// </summary>
    /// <param name="content">
    ///     The message raw binary content (body).
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint information.
    /// </param>
    public OutboxMessage(
        byte[]? content,
        IEnumerable<MessageHeader>? headers,
        OutboxMessageEndpoint endpoint)
    {
        Content = content;
        Headers = headers?.AsReadOnlyCollection();
        Endpoint = endpoint;
    }

    /// <summary>
    ///     Gets the message raw binary content (body).
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed")]
    public byte[]? Content { get; }

    /// <summary>
    ///     Gets the message headers.
    /// </summary>
    public IReadOnlyCollection<MessageHeader>? Headers { get; }

    /// <summary>
    ///     Gets the target endpoint.
    /// </summary>
    public OutboxMessageEndpoint Endpoint { get; }
}
