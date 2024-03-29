// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Storage;

/// <summary>
///     Adds the relational storage specific methods to the <see cref="SilverbackContext" />.
/// </summary>
// TODO: Test
public static class SilverbackContextRelationalStorageExtensions
{
    /// <summary>
    ///     Specifies the <see cref="DbTransaction" /> to be used for storage operations.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction.
    /// </param>
    /// <returns>
    ///    The <see cref="IStorageTransaction" />.
    /// </returns>
    public static IStorageTransaction EnlistDbTransaction(this SilverbackContext context, DbTransaction transaction) =>
        new DbTransactionWrapper(transaction, context);

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
    /// <returns>
    ///     The transaction or <c>null</c>.
    /// </returns>
    public static T? GetActiveDbTransaction<T>(this SilverbackContext? context)
        where T : DbTransaction => context != null && context.TryGetActiveDbTransaction(out T? transaction) ? transaction : null;

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
        if (!context.TryGetStorageTransaction(out IStorageTransaction? storageTransaction))
        {
            transaction = null;
            return false;
        }

        if (storageTransaction.UnderlyingTransaction is not T dbTransaction)
        {
            throw new InvalidOperationException(
                $"The current transaction ({storageTransaction.UnderlyingTransaction.GetType().Name}) is not a {typeof(T).Name}. " +
                "Silverback must be configured to use the same storage as the one used by the application.");
        }

        transaction = dbTransaction;
        return true;
    }
}
