// Copyright (c) 2024 Sergio Aquilini
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
    /// <param name="endpointName">
    ///     The endpoint name.
    /// </param>
    public OutboxMessage(
        byte[]? content,
        IEnumerable<MessageHeader>? headers,
        string endpointName)
    {
        Content = content;
        Headers = headers?.AsReadOnlyCollection();
        EndpointName = endpointName;
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
    ///     Gets the destination endpoint name.
    /// </summary>
    public string EndpointName { get; }
}
