// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Storage;

/// <summary>
///     Adds the storage specific methods to the <see cref="SilverbackContext" />.
/// </summary>
// TODO: Test?
public static class SilverbackContextStorageExtensions
{
    private static readonly Guid StorageTransactionObjectTypeId = new("f6c8c224-392a-4d57-8344-46e190624e3c");

    /// <summary>
    ///     Specifies the transaction to be used for storage operations.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction.
    /// </param>
    public static void EnlistTransaction(this SilverbackContext context, IStorageTransaction transaction) =>
        Check.NotNull(context, nameof(context)).AddObject(StorageTransactionObjectTypeId, transaction);

    /// <summary>
    ///     Checks whether a storage transaction is set and returns it.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transaction">
    ///     The transaction.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public static bool TryGetStorageTransaction(
        this SilverbackContext context,
        [NotNullWhen(true)] out IStorageTransaction? transaction) =>
        Check.NotNull(context, nameof(context)).TryGetObject(StorageTransactionObjectTypeId, out transaction);

    /// <summary>
    ///     Clears the storage transaction.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    public static void RemoveTransaction(this SilverbackContext context) =>
        Check.NotNull(context, nameof(context)).RemoveObject(StorageTransactionObjectTypeId);
}
