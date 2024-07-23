// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Wraps the <see cref="Confluent.SchemaRegistry.SchemaRegistryConfig" /> adding the Silverback specific settings.
/// </summary>
public sealed partial record KafkaSchemaRegistryConfiguration : IValidatableSettings
{
    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(Url))
            throw new BrokerConfigurationException($"At least 1 {nameof(Url)} is required to connect with the schema registry.");
    }

    internal SchemaRegistryConfig ToConfluentConfig() => MapCore();
}
