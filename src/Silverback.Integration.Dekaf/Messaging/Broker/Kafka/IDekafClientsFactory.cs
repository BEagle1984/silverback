// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Dekaf.Consumer;
using Dekaf.Producer;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Creates the underlying Dekaf clients. It is the seam used to replace the real clients, for example with the
///     in-memory clients of <c>Dekaf.Testing</c>.
/// </summary>
public interface IDekafClientsFactory
{
    /// <summary>
    ///     Creates a new (not yet initialized) consumer.
    /// </summary>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    /// <param name="rebalanceListener">
    ///     The listener to be notified about the partitions assignment changes.
    /// </param>
    /// <returns>
    ///     The consumer.
    /// </returns>
    IKafkaConsumer<byte[], byte[]> CreateConsumer(KafkaConsumerConfiguration configuration, IConsumerAwareRebalanceListener rebalanceListener);

    /// <summary>
    ///     Creates a new (not yet initialized) producer.
    /// </summary>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <returns>
    ///     The producer.
    /// </returns>
    IKafkaProducer<byte[], byte[]> CreateProducer(KafkaProducerConfiguration configuration);
}
