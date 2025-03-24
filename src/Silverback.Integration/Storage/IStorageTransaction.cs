// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Storage;

/// <summary>
///     Represents a transaction to be used for storage operations.
/// </summary>
public interface IStorageTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Gets the underlying transaction object.
    /// </summary>
    object UnderlyingTransaction { get; }

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    void Commit();

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task CommitAsync();

    /// <summary>
    ///     Aborts the transaction.
    /// </summary>
    void Rollback();

    /// <summary>
    ///     Aborts the transaction.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task RollbackAsync();
}
