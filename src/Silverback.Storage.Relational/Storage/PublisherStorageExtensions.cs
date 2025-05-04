// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Storage;

/// <summary>
///     Adds the storage-specific methods to the <see cref="Publisher" />.
/// </summary>
public static class PublisherStorageExtensions
{
    /// <summary>
    ///     Specifies the transaction to be used for storage operations.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction to be used.
    /// </param>
    public static void EnlistTransaction(this IPublisher publisher, IStorageTransaction transaction) =>
        Check.NotNull(publisher, nameof(publisher)).Context.EnlistTransaction(transaction);

    /// <summary>
    ///     Specifies the <see cref="DbTransaction" /> to be used for storage operations.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="dbTransaction">
    ///     The transaction to be used.
    /// </param>
    /// <param name="ownTransaction">
    ///     A value indicating whether the transaction should be disposed when the <see cref="IStorageTransaction" /> is disposed.
    /// </param>
    /// <returns>
    ///     The <see cref="IStorageTransaction" />.
    /// </returns>
    public static IStorageTransaction EnlistDbTransaction(this IPublisher publisher, DbTransaction dbTransaction, bool ownTransaction = true) =>
        Check.NotNull(publisher, nameof(publisher)).Context.EnlistDbTransaction(dbTransaction, ownTransaction);
}
