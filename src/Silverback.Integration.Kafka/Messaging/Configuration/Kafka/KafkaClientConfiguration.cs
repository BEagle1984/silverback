// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> and contains the properties shared between the
///     <see cref="KafkaClientProducerConfiguration" /> and <see cref="KafkaClientConsumerConfiguration" />.
/// </summary>
public record KafkaClientConfiguration : KafkaClientConfiguration<ClientConfig>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConfiguration" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConfiguration" />.
    /// </param>
    public KafkaClientConfiguration(KafkaClientConfiguration? clientConfiguration = null)
        : base(clientConfiguration)
    {
    }

    internal KafkaClientConfiguration(ClientConfig clientConfig)
        : base(clientConfig)
    {
    }

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public override void Validate()
    {
        // Don't validate anything, leave it to the KafkaProducerConfig and KafkaConsumerConfig
    }
}
