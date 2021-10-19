// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Creates and stores the <see cref="IProducer{TKey,TValue}" /> instances in order to reuse them for the same configuration.
/// </summary>
public interface IConfluentProducersCache
{
    /// <summary>
    ///     Gets an <see cref="IProducer{TKey,TValue}" /> compatible with the specified configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The Confluent producer configuration.
    /// </param>
    /// <param name="owner">
    ///     The <see cref="KafkaProducer" /> to be linked to the new producer being created.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer{TKey,TValue}" />.
    /// </returns>
    IProducer<byte[]?, byte[]?> GetProducer(KafkaClientProducerConfiguration configuration, KafkaProducer owner);

    /// <summary>
    ///     Disposes the <see cref="IProducer{TKey,TValue}" /> for the specified configuration and removes it from the cache.
    /// </summary>
    /// <param name="configuration">
    ///     The Confluent producer configuration.
    /// </param>
    void DisposeProducer(KafkaClientProducerConfiguration configuration);
}
