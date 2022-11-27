// Copyright (c) 2020 Sergio Aquilini
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
    ///     Specifies an existing <see cref="DbTransaction" /> to be used for database operations.
    /// </summary>
    /// <param name="publisher">
    ///     The publisher.
    /// </param>
    /// <param name="dbTransaction">
    ///     The transaction to be used.
    /// </param>
    public static void EnlistTransaction(this IPublisherBase publisher, DbTransaction dbTransaction) =>
        Check.NotNull(publisher, nameof(publisher)).Context.EnlistTransaction(dbTransaction);
}
