// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Creates the <see cref="IDekafProducerWrapper" /> instances.
/// </summary>
internal interface IDekafProducerWrapperFactory
{
    /// <summary>
    ///     Creates a new <see cref="IDekafProducerWrapper" />.
    /// </summary>
    /// <param name="name">
    ///     The name of the client.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="KafkaProducerConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IDekafProducerWrapper" />.
    /// </returns>
    IDekafProducerWrapper Create(string name, KafkaProducerConfiguration configuration);
}
