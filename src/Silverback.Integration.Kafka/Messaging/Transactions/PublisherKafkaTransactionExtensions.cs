// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Transactions;

/// <summary>
///     Adds the <see cref="InitKafkaTransaction" /> method to the <see cref="Publisher" />.
/// </summary>
public static class PublisherKafkaTransactionExtensions
{
    /// <summary>
    ///     Initializes the Kafka transaction.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="transactionalIdSuffix">
    ///     The optional suffix to be appended to the transactional Id. This must be used to allow multiple concurrent transactions.
    /// </param>
    /// <returns>
    ///     The created <see cref="IKafkaTransaction" />.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    ///     A Kafka transaction has already been initialized in the context and not disposed yet.
    /// </exception>
    public static IKafkaTransaction InitKafkaTransaction(this IPublisher publisher, string? transactionalIdSuffix = null) =>
        Check.NotNull(publisher).Context.InitKafkaTransaction(transactionalIdSuffix);
}
