// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging;

/// <summary>
///     The Kafka producer configuration.
/// </summary>
public sealed record KafkaProducerConfiguration : ProducerConfiguration<KafkaProducerEndpoint>
{
    /// <summary>
    ///     Gets the Kafka client configuration. This is actually an extension of the configuration dictionary provided by the
    ///     Confluent.Kafka library.
    /// </summary>
    public KafkaClientProducerConfiguration Client { get; init; } = new();

    /// <inheritdoc cref="ProducerConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Client == null)
            throw new EndpointConfigurationException("The client configuration is required.");

        Client.Validate();
    }
}
