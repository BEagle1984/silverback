﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     Coordinates the in-memory transactions for the <see cref="IMockedConfluentProducer" />.
/// </summary>
public interface IInMemoryTransactionManager
{
    /// <summary>
    ///     Initializes the transaction manager for the specified transactionalId.
    /// </summary>
    /// <param name="transactionalId">
    ///     The transactionalId of the producer.
    /// </param>
    /// <returns>
    ///     The transactional unique identifier.
    /// </returns>
    Guid InitTransaction(string transactionalId);

    /// <summary>
    ///     Begins a new transaction.
    /// </summary>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    void BeginTransaction(Guid transactionalUniqueId);

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    void CommitTransaction(Guid transactionalUniqueId);

    /// <summary>
    ///     Aborts the transaction.
    /// </summary>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    void AbortTransaction(Guid transactionalUniqueId);

    /// <summary>
    ///     Gets a value indicating whether a transaction is pending for the specified transactional unique identifier.
    /// </summary>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    /// <returns>
    ///     <c>true</c> if a transaction is pending, otherwise <c>false</c>.
    /// </returns>
    bool IsTransactionPending(Guid transactionalUniqueId);
}