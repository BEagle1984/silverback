// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Silverback.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Wraps the <see cref="Confluent.SchemaRegistry.SchemaRegistryConfig" /> adding the Silverback specific settings.
/// </summary>
public sealed partial record KafkaSchemaRegistryConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaSchemaRegistryConfiguration" /> class.
    /// </summary>
    public KafkaSchemaRegistryConfiguration()
        : this((SchemaRegistryConfig?)null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaSchemaRegistryConfiguration" /> class.
    /// </summary>
    /// <param name="original">
    ///     The <see cref="KafkaSchemaRegistryConfiguration" /> to be cloned.
    /// </param>
    public KafkaSchemaRegistryConfiguration(KafkaSchemaRegistryConfiguration original)
    {
        SchemaRegistryConfig = Check.NotNull(original, nameof(original)).SchemaRegistryConfig.ShallowCopy();
    }

    internal KafkaSchemaRegistryConfiguration(SchemaRegistryConfig? consumerConfig)
    {
        SchemaRegistryConfig = consumerConfig ?? new SchemaRegistryConfig();
    }

    /// <summary>
    ///     Gets the wrapped <see cref="Confluent.SchemaRegistry.SchemaRegistryConfig" />.
    /// </summary>
    private SchemaRegistryConfig SchemaRegistryConfig { get; }

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(Url))
            throw new BrokerConfigurationException($"At least 1 {nameof(Url)} is required to connect with the schema registry.");
    }

    internal SchemaRegistryConfig GetConfluentSchemaRegistryConfig() => SchemaRegistryConfig;
}
