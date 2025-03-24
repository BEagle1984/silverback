// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> and contains the properties shared between the
///     <see cref="KafkaProducerConfiguration" /> and <see cref="KafkaConsumerConfiguration" />.
/// </summary>
/// <typeparam name="TConfluentConfig">
///     The type of the wrapped <see cref="ClientConfig" />.
/// </typeparam>
public abstract partial record KafkaClientConfiguration<TConfluentConfig> : IValidatableSettings
    where TConfluentConfig : ClientConfig, new()
{
    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public virtual void Validate()
    {
        if (string.IsNullOrEmpty(BootstrapServers))
            throw new BrokerConfigurationException($"The {nameof(BootstrapServers)} are required to connect with the message broker.");
    }

    internal abstract TConfluentConfig ToConfluentConfig();
}
