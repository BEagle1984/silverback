// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Adds the storage-specific methods to the <see cref="KafkaOffsetStoreScope" />.
/// </summary>
public static class KafkaOffsetStoreScopeRelationalExtensions
{
    /// <summary>
    ///     Specifies the transaction to be used for storage operations.
    /// </summary>
    /// <param name="scope">
    ///     The <see cref="KafkaOffsetStoreScope" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction to be used.
    /// </param>
    public static void EnlistTransaction(this KafkaOffsetStoreScope scope, IStorageTransaction transaction) =>
        Check.NotNull(scope, nameof(scope)).SilverbackContext.EnlistTransaction(transaction);

    /// <summary>
    ///     Specifies the <see cref="DbTransaction" /> to be used for storage operations.
    /// </summary>
    /// <param name="scope">
    ///     The <see cref="KafkaOffsetStoreScope" />.
    /// </param>
    /// <param name="dbTransaction">
    ///     The transaction to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="IStorageTransaction" />.
    /// </returns>
    public static IStorageTransaction EnlistTransaction(this KafkaOffsetStoreScope scope, DbTransaction dbTransaction) =>
        Check.NotNull(scope, nameof(scope)).SilverbackContext.EnlistDbTransaction(dbTransaction);
}
