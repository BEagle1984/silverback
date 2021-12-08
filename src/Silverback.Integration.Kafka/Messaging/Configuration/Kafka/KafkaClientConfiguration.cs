// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> and contains the properties shared between the
///     <see cref="KafkaClientProducerConfiguration" /> and <see cref="KafkaClientConsumerConfiguration" />.
/// </summary>
public partial record KafkaClientConfiguration
{
    private readonly ClientConfig _clientConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfiguration" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConfiguration" />.
    /// </param>
    public KafkaClientConfiguration(ClientConfig? clientConfig = null)
    {
        //_clientConfig = clientConfig?.Clone() ?? new ClientConfig();
        _clientConfig = clientConfig ?? new ClientConfig();
    }

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public virtual void Validate()
    {
        // Don't validate anything, leave it to the KafkaProducerConfig and KafkaConsumerConfig
    }

    internal ClientConfig GetConfluentClientConfig() => _clientConfig;
}
