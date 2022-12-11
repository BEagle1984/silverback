// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Adds the Kafka offset store specific methods to the <see cref="SilverbackContext" />.
/// </summary>
// TODO: Test?
public static class SilverbackContextKafkaOffsetSToreExtensions
{
    private const int OffsetStoreObjectTypeId = 1000;

    /// <summary>
    ///     Stores the specified storage transaction.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="scope">
    ///     The scope.
    /// </param>
    public static void SetKafkaOffsetStoreScope(this SilverbackContext context, KafkaOffsetStoreScope scope) =>
        Check.NotNull(context, nameof(context)).SetObject(OffsetStoreObjectTypeId, scope);

    /// <summary>
    ///     Checks whether a storage transaction is set and returns it.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="scope">
    ///     The scope.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public static bool TryGetKafkaOffsetStoreScope(
        this SilverbackContext context,
        [NotNullWhen(true)] out KafkaOffsetStoreScope? scope)
    {
        if (Check.NotNull(context, nameof(context)).TryGetObject(OffsetStoreObjectTypeId, out object? scopeObject))
        {
            scope = (KafkaOffsetStoreScope)scopeObject;
            return true;
        }

        scope = null;
        return false;
    }
}
