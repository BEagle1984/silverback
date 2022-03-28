// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <inheritdoc cref="OutboxMessage" />
// TODO: Move to Silverback.Storage.RelationalDatabase
public class DbOutboxMessage : OutboxMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DbOutboxMessage" /> class.
    /// </summary>
    /// <param name="id">
    ///     The primary key of the database record.
    /// </param>
    /// <param name="messageType">
    ///     The type of the message.
    /// </param>
    /// <param name="content">
    ///     The message raw binary content (body).
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint information.
    /// </param>
    public DbOutboxMessage(
        long id,
        Type? messageType,
        byte[]? content,
        IEnumerable<MessageHeader>? headers,
        OutboxMessageEndpoint endpoint)
        : base(messageType, content, headers, endpoint)
    {
        Id = id;
    }

    /// <summary>
    ///     Gets the primary key of the database record.
    /// </summary>
    public long Id { get; }
}
