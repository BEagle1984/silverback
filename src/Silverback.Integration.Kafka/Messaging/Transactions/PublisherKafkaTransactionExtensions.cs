// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

/// <summary>
///     Adds the <see cref="InitKafkaTransaction" /> method to the <see cref="Publisher" />.
/// </summary>
// TODO: Test
public static class PublisherKafkaTransactionExtensions
{
    /// <summary>
    ///     Initializes the Kafka transaction.
    /// </summary>
    /// <param name="publisher">
    ///     The publisher.
    /// </param>
    /// <param name="transactionalIdSuffix">
    ///    The optional suffix to be appended to the transactional Id. This must be used to allow multiple concurrent transactions.
    /// </param>
    /// <returns>
    ///     The created <see cref="IKafkaTransaction" />.
    /// </returns>
    public static IKafkaTransaction InitKafkaTransaction(this IPublisherBase publisher, string? transactionalIdSuffix = null) =>
        Check.NotNull(publisher, nameof(publisher)).Context.InitKafkaTransaction(transactionalIdSuffix);
}
