// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <inheritdoc cref="OutboxMessage" />
public class DbOutboxMessage : OutboxMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DbOutboxMessage" /> class.
    /// </summary>
    /// <param name="id">
    ///     The primary key of the database record.
    /// </param>
    /// <param name="content">
    ///     The message raw binary content (body).
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpointName">
    ///     The endpoint name.
    /// </param>
    public DbOutboxMessage(
        long id,
        byte[]? content,
        IEnumerable<MessageHeader>? headers,
        string endpointName)
        : base(content, headers, endpointName)
    {
        Id = id;
    }

    /// <summary>
    ///     Gets the primary key of the database record.
    /// </summary>
    public long Id { get; }
}
