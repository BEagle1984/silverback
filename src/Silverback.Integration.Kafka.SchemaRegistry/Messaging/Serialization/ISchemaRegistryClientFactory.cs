// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The factory used to create <see cref="ISchemaRegistryClient" /> instances.
/// </summary>
public interface ISchemaRegistryClientFactory
{
    /// <summary>
    ///     Returns a <see cref="ISchemaRegistryClient" /> instance for the specified configuration.
    /// </summary>
    /// <param name="schemaRegistryConfiguration">
    ///     The schema registry configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ISchemaRegistryClient" />.
    /// </returns>
    ISchemaRegistryClient GetClient(KafkaSchemaRegistryConfiguration schemaRegistryConfiguration);
}
