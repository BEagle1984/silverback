// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerEndpointConfigurationFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaProducerEndpointConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSerializerIsNull()
    {
        KafkaProducerEndpointConfiguration configuration = GetValidConfiguration() with { Serializer = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNull()
    {
        KafkaProducerEndpointConfiguration configuration = GetValidConfiguration() with { Endpoint = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenStrategyIsNull()
    {
        KafkaProducerEndpointConfiguration configuration = GetValidConfiguration() with { Strategy = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    private static KafkaProducerEndpointConfiguration GetValidConfiguration() => new()
    {
        Endpoint = new KafkaStaticProducerEndpointResolver("topic1")
    };
}
