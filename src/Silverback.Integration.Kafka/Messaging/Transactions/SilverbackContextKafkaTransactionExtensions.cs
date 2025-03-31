// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

/// <summary>
///     Adds the <see cref="InitKafkaTransaction" /> method to the <see cref="SilverbackContext" />.
/// </summary>
public static class SilverbackContextKafkaTransactionExtensions
{
    private static readonly Guid KafkaTransactionObjectTypeId = new("638570b5-501f-4963-992a-3c6c1c465f51");

    /// <summary>
    ///     Initializes the Kafka transaction.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <param name="transactionalIdSuffix">
    ///     The optional suffix to be appended to the transactional Id. This must be used to allow multiple concurrent transactions.
    /// </param>
    /// <returns>
    ///     The created <see cref="IKafkaTransaction" />.
    /// </returns>
    public static IKafkaTransaction InitKafkaTransaction(this ISilverbackContext context, string? transactionalIdSuffix = null)
    {
        Check.NotNull(context, nameof(context));

        if (context.TryGetConsumerPipelineContext(out ConsumerPipelineContext? consumerPipelineContext) &&
            consumerPipelineContext is
            {
                Consumer: KafkaConsumer { Configuration.ProcessPartitionsIndependently: true },
                Envelope.BrokerMessageIdentifier: KafkaOffset offset
            })
        {
            transactionalIdSuffix = $"{transactionalIdSuffix}|{offset.TopicPartition.Topic}[{offset.TopicPartition.Partition.Value}]";
        }

        return new KafkaTransaction(context, transactionalIdSuffix);
    }

    internal static void AddKafkaTransaction(this ISilverbackContext context, KafkaTransaction kafkaTransaction) =>
        Check.NotNull(context, nameof(context)).AddObject(KafkaTransactionObjectTypeId, kafkaTransaction);

    internal static void RemoveKafkaTransaction(this ISilverbackContext context) =>
        Check.NotNull(context, nameof(context)).RemoveObject(KafkaTransactionObjectTypeId);

    internal static KafkaTransaction GetKafkaTransaction(this ISilverbackContext context) =>
        Check.NotNull(context, nameof(context)).GetObject<KafkaTransaction>(KafkaTransactionObjectTypeId);
}
