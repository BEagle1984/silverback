// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     Exposes the methods to write to the outbox.
/// </summary>
/// <remarks>
///     Used by the <see cref="OutboxProduceStrategy" />.
/// </remarks>
public interface IOutboxWriter
{
    /// <summary>
    ///     Adds the message contained in the specified envelope to the outbox.
    /// </summary>
    /// <param name="outboxMessage">
    ///     The message to be stored in the outbox.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task AddAsync(OutboxMessage outboxMessage);
}
