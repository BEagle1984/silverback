// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka
{
    /// <summary>
    ///     Creates and stores the <see cref="IProducer{TKey,TValue}" /> instances in order to reuse them for the
    ///     same <see cref="KafkaProducerConfig" /> configuration.
    /// </summary>
    public interface IConfluentProducersCache
    {
        /// <summary>
        ///     Gets an <see cref="IProducer{TKey,TValue}" /> compatible with the specified
        ///     <see cref="KafkaProducerConfig" />.
        /// </summary>
        /// <param name="config">
        ///     The <see cref="KafkaProducerConfig" />.
        /// </param>
        /// <param name="owner">
        ///     The <see cref="KafkaProducer" /> to be linked to the new producer being created.
        /// </param>
        /// <returns>
        ///     The <see cref="IProducer{TKey,TValue}" />.
        /// </returns>
        IProducer<byte[]?, byte[]?> GetProducer(KafkaProducerConfig config, KafkaProducer owner);

        /// <summary>
        ///     Disposes the <see cref="IProducer{TKey,TValue}" /> for the specified
        ///     <see cref="KafkaProducerConfig" /> and removes it from the cache.
        /// </summary>
        /// <param name="config">
        ///     The <see cref="KafkaProducerConfig" />.
        /// </param>
        void DisposeProducer(KafkaProducerConfig config);
    }
}
