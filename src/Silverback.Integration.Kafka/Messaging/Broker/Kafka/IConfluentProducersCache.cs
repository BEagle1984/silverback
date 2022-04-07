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
        /// <param name="ownerProducer">
        ///     The <see cref="KafkaProducer" /> that needs the producer.
        /// </param>
        /// <returns>
        ///     The <see cref="IProducer{TKey,TValue}" />.
        /// </returns>
        IProducer<byte[]?, byte[]?> GetProducer(KafkaProducer ownerProducer);

        /// <summary>
        ///     Disposes the <see cref="IProducer{TKey,TValue}" /> for the specified
        ///     <see cref="KafkaProducerConfig" /> and removes it from the cache.
        /// </summary>
        /// <param name="ownerProducer">
        ///     The <see cref="KafkaProducer" /> that owns the producer to be disposed.
        /// </param>
        void DisposeProducer(KafkaProducer ownerProducer);
    }
}
