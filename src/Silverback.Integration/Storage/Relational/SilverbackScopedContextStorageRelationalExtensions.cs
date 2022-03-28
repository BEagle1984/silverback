// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Storage.Relational;

/// <summary>
///     Adds the relational storage specific methods to the <see cref="SilverbackContext" />.
/// </summary>
// TODO: Test
public static class SilverbackScopedContextStorageRelationalExtensions
{
    /// <summary>
    ///     Checks whether an active <see cref="DbTransaction" /> is set and returns it.
    /// </summary>
    /// <typeparam name="T">
    ///     The expected type of the <see cref="DbTransaction" />. An <see cref="InvalidOperationException" /> will be thrown if the type of
    ///     the stored transaction object is not compatible.
    /// </typeparam>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public static bool TryGetActiveDbTransaction<T>(this SilverbackContext context, [NotNullWhen(true)] out T? transaction)
        where T : DbTransaction
    {
        if (!context.TryGetStorageTransaction(out object? storageTransaction))
        {
            transaction = null;
            return false;
        }

        if (storageTransaction is not DbTransaction dbTransaction)
        {
            throw new InvalidOperationException(
                $"The current transaction ({storageTransaction?.GetType().Name}) is not a DbTransaction. " +
                "Silverback must be configured to use the same storage as the one used by the application.");
        }

        if (dbTransaction.Connection == null || !dbTransaction.Connection.State.HasFlag(ConnectionState.Open))
        {
            transaction = null;
            return false;
        }

        if (dbTransaction is not T typedDbTransaction)
        {
            throw new InvalidOperationException(
                $"The connection associated with the current transaction ({dbTransaction.Connection.GetType().Name}) is not " +
                $"a {typeof(T).Name}. Silverback must be configured to use the same storage as the one used by the application.");
        }

        transaction = typedDbTransaction;
        return true;
    }
}
