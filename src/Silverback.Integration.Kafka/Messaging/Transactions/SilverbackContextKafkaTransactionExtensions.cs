// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

/// <summary>
///     Adds the <see cref="InitKafkaTransaction" /> method to the <see cref="SilverbackContext" />.
/// </summary>
// TODO: Test?
public static class SilverbackContextKafkaTransactionExtensions
{
    private static readonly Guid KafkaTransactionObjectTypeId = new("f6c8c224-392a-4d57-8344-46e190624e3c");

    /// <summary>
    ///     Initializes the Kafka transaction.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <param name="transactionalIdSuffix">
    ///    The optional suffix to be appended to the transactional Id. This must be used to allow multiple concurrent transactions.
    /// </param>
    /// <returns>
    ///     The created <see cref="IKafkaTransaction" />.
    /// </returns>
    public static IKafkaTransaction InitKafkaTransaction(this SilverbackContext context, string? transactionalIdSuffix = null) =>
        new KafkaTransaction(context, transactionalIdSuffix);

    internal static void AddKafkaTransaction(this SilverbackContext context, KafkaTransaction kafkaTransaction) =>
        Check.NotNull(context, nameof(context)).AddObject(KafkaTransactionObjectTypeId, kafkaTransaction);

    internal static void RemoveKafkaTransaction(this SilverbackContext context) =>
        Check.NotNull(context, nameof(context)).RemoveObject(KafkaTransactionObjectTypeId);

    internal static KafkaTransaction? GetKafkaTransaction(this SilverbackContext context) =>
        (KafkaTransaction?)Check.NotNull(context, nameof(context)).GetObject(KafkaTransactionObjectTypeId);

    // internal static bool TryGetKafkaTransaction(this SilverbackContext context, [NotNullWhen(true)] out IKafkaTransaction? transaction)
    // {
    //     Check.NotNull(context, nameof(context));
    //
    //     if (context.TryGetObject(KafkaTransactionObjectTypeId, out object? transactionObject))
    //     {
    //         transaction = (IKafkaTransaction)transactionObject;
    //         return true;
    //     }
    //
    //     transaction = null;
    //     return false;
    // }
}
