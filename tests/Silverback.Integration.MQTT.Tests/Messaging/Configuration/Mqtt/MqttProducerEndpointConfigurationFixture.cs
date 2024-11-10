// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttProducerEndpointConfigurationFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttProducerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSerializerIsNull()
    {
        MqttProducerEndpointConfiguration configuration = GetValidConfiguration() with { Serializer = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNull()
    {
        MqttProducerEndpointConfiguration configuration = GetValidConfiguration() with { EndpointResolver = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenChunkingIsEnabled()
    {
        MqttProducerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Chunk = new ChunkSettings { Size = 42 }
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Chunking is currently not supported for MQTT.");
    }

    private static MqttProducerEndpointConfiguration GetValidConfiguration() =>
        new()
        {
            EndpointResolver = new MqttStaticProducerEndpointResolver("topic")
        };
}
