// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaProducerConfiguration" />.
/// </summary>
public class KafkaClientConfigurationBuilder
    : KafkaClientConfigurationBuilder<KafkaClientConfiguration, ClientConfig, KafkaClientConfigurationBuilder>
{
    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TConfig,TConfluentConfig,TBuilder}.This" />
    protected override KafkaClientConfigurationBuilder This => this;

    /// <summary>
    ///     Builds the <see cref="KafkaClientConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaClientConfiguration" />.
    /// </returns>
    public KafkaClientConfiguration Build()
    {
        KafkaClientConfiguration configuration = BuildCore();
        configuration.Validate();
        return configuration;
    }
}
