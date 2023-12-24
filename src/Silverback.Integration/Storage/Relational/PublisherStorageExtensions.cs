// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Data.Common;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Storage.Relational;

/// <summary>
///     Adds the storage specific methods to the <see cref="Publisher" />.
/// </summary>
// TODO: Test
public static class PublisherStorageExtensions
{
    /// <summary>
    ///     Specifies the transaction to be used for storage operations.
    /// </summary>
    /// <param name="publisher">
    ///     The publisher.
    /// </param>
    /// <param name="transaction">
    ///     The transaction to be used.
    /// </param>
    public static void EnlistTransaction(this IPublisherBase publisher, IStorageTransaction transaction) =>
        Check.NotNull(publisher, nameof(publisher)).Context.EnlistTransaction(transaction);

    /// <summary>
    ///     Specifies the <see cref="DbTransaction" /> to be used for storage operations.
    /// </summary>
    /// <param name="publisher">
    ///     The publisher.
    /// </param>
    /// <param name="dbTransaction">
    ///     The transaction to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="IStorageTransaction" />.
    /// </returns>
    public static IStorageTransaction EnlistDbTransaction(this IPublisherBase publisher, DbTransaction dbTransaction) =>
        Check.NotNull(publisher, nameof(publisher)).Context.EnlistDbTransaction(dbTransaction);
}
