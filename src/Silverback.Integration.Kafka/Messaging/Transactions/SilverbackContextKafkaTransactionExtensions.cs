// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.ContextEnrichment;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

/// <summary>
///     Adds the <see cref="InitKafkaTransaction" /> method to the <see cref="SilverbackContext" />.
/// </summary>
public static class SilverbackContextKafkaTransactionExtensions
{
    private static readonly Guid KafkaTransactionObjectTypeId = new("f6c8c224-392a-4d57-8344-46e190624e3c");

    /// <summary>
    ///     Initializes the Kafka transaction.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <param name="transactionalIdSuffix">
    ///    The optional suffix to be appended to the transactional Id. This must be used to allow multiple concurrent transactions.
    /// </param>
    /// <returns>
    ///     The created <see cref="IKafkaTransaction" />.
    /// </returns>
    public static IKafkaTransaction InitKafkaTransaction(this ISilverbackContext context, string? transactionalIdSuffix = null)
    {
        Check.NotNull(context, nameof(context));

        if (context.TryGetConsumedPartition(out ConsumedTopicPartition? consumedPartition) && consumedPartition.ProcessedIndependently)
            transactionalIdSuffix = $"{transactionalIdSuffix}|{consumedPartition.TopicPartition.Topic}[{consumedPartition.TopicPartition.Partition.Value}]";

        return new KafkaTransaction(context, transactionalIdSuffix);
    }

    internal static void AddKafkaTransaction(this ISilverbackContext context, KafkaTransaction kafkaTransaction) =>
        Check.NotNull(context, nameof(context)).AddObject(KafkaTransactionObjectTypeId, kafkaTransaction);

    internal static void RemoveKafkaTransaction(this ISilverbackContext context) =>
        Check.NotNull(context, nameof(context)).RemoveObject(KafkaTransactionObjectTypeId);

    internal static KafkaTransaction GetKafkaTransaction(this ISilverbackContext context) =>
        Check.NotNull(context, nameof(context)).GetObject<KafkaTransaction>(KafkaTransactionObjectTypeId);
}
