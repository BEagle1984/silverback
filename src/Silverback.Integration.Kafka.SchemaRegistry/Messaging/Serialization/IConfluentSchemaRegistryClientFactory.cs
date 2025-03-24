// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The factory used to create <see cref="ISchemaRegistryClient" /> instances.
/// </summary>
public interface IConfluentSchemaRegistryClientFactory
{
    /// <summary>
    ///     Returns an <see cref="ISchemaRegistryClient" /> instance for the specified configuration.
    /// </summary>
    /// <param name="builderAction">
    ///     An action that takes a <see cref="KafkaSchemaRegistryConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ISchemaRegistryClient" />.
    /// </returns>
    ISchemaRegistryClient GetClient(Action<KafkaSchemaRegistryConfigurationBuilder> builderAction);

    /// <summary>
    ///     Returns an <see cref="ISchemaRegistryClient" /> instance for the specified configuration.
    /// </summary>
    /// <param name="schemaRegistryConfiguration">
    ///     The schema registry configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ISchemaRegistryClient" />.
    /// </returns>
    ISchemaRegistryClient GetClient(KafkaSchemaRegistryConfiguration schemaRegistryConfiguration);
}
