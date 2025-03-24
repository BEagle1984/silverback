// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Creates <see cref="IConfluentProducerWrapper" /> instances.
/// </summary>
public interface IConfluentProducerWrapperFactory
{
    /// <summary>
    ///     Creates a new <see cref="IConfluentProducerWrapper" />.
    /// </summary>
    /// <param name="name">
    ///     The name of the client.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="KafkaProducerConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerWrapper" />.
    /// </returns>
    IConfluentProducerWrapper Create(string name, KafkaProducerConfiguration configuration);
}
