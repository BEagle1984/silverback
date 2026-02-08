// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The Kafka producer endpoint configuration.
/// </summary>
public sealed record KafkaProducerEndpointConfiguration : ProducerEndpointConfiguration<KafkaProducerEndpoint>
{
    /// <summary>
    ///     Gets the key serializer.
    /// </summary>
    public ISimpleSerializer KeySerializer { get; init; } = DefaultSerializers.SimpleString;

    /// <inheritdoc cref="ProducerEndpointConfiguration{TEndpoint}.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (KeySerializer == null)
            throw new BrokerConfigurationException("A key serializer is required.");
    }
}
