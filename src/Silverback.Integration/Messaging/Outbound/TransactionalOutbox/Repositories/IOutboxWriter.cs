// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

/// <summary>
///     Exposes the methods to write to the outbox. Used by the <see cref="OutboxProduceStrategy" />.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="message">
    ///     The message to be delivered.
    /// </param>
    /// <param name="messageBytes">
    ///     The actual serialized message to be delivered.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpointRawName">
    ///     The name of the target endpoint.
    /// </param>
    /// <param name="endpointFriendlyName">
    ///     The optional friendly name of the target endpoint.
    /// </param>
    /// <param name="serializedEndpoint">
    ///     The serialized endpoint. This is necessary for dynamic endpoints only.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WriteAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        string endpointRawName,
        string? endpointFriendlyName,
        byte[]? serializedEndpoint);

    /// <summary>
    ///     Called to commit the transaction, storing the pending messages to the outbox.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task CommitAsync();

    /// <summary>
    ///     Called to rollback the transaction, preventing the pending messages to be stored in the outbox.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task RollbackAsync();
}
