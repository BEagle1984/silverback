// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The Kafka producer endpoint configuration.
/// </summary>
public sealed record KafkaProducerEndpointConfiguration : ProducerEndpointConfiguration<KafkaProducerEndpoint>
{
    // TODO: Add missing tests
    
    
//  public Type KeyType { get; init; } = typeof(string);

    public ISimpleSerializer KeySerializer { get; init; } = DefaultSerializers.SimpleString;

    protected override void ValidateCore()
    {
        base.ValidateCore();

        // if (KeyType == null)
        //     throw new BrokerConfigurationException("The key type is required.");

        if (KeySerializer == null)
            throw new BrokerConfigurationException("The key serializer is required.");

        // TODO: Worth checking key type and serializer compatibility?
    }
}
