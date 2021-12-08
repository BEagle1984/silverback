// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaProducerConfigurationTests
{
    [Fact]
    public void Equals_SameEndpointInstance_TrueReturned()
    {
        KafkaProducerConfiguration configuration = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 42),
            Client = new KafkaClientProducerConfiguration
            {
                Acks = Acks.Leader
            }
        };

        configuration.Equals(configuration).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameConfiguration_TrueReturned()
    {
        KafkaProducerConfiguration configuration1 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 42),
            Client = new KafkaClientProducerConfiguration
            {
                Acks = Acks.Leader
            }
        };
        KafkaProducerConfiguration configuration2 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 42),
            Client = new KafkaClientProducerConfiguration
            {
                Acks = Acks.Leader
            }
        };

        configuration1.Equals(configuration2).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameDynamicEndpoint_TrueReturned()
    {
        KafkaDynamicProducerEndpointResolver configuration = new("topic", _ => 42);

        KafkaProducerConfiguration configuration1 = new()
        {
            Endpoint = configuration
        };
        KafkaProducerConfiguration configuration2 = new()
        {
            Endpoint = configuration
        };

        configuration1.Equals(configuration2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        KafkaProducerConfiguration configuration1 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic1", 42)
        };
        KafkaProducerConfiguration configuration2 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic2", 42)
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentPartition_FalseReturned()
    {
        KafkaProducerConfiguration configuration1 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 1)
        };
        KafkaProducerConfiguration configuration2 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 2)
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentConfiguration_FalseReturned()
    {
        KafkaProducerConfiguration configuration1 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 42),
            Client = new KafkaClientProducerConfiguration
            {
                Acks = Acks.Leader
            }
        };
        KafkaProducerConfiguration configuration2 = new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic", 42),
            Client = new KafkaClientProducerConfiguration
            {
                Acks = Acks.All
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidEndpoint_NoExceptionThrown()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_InvalidConfiguration_ExceptionThrown()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            Client = new KafkaClientProducerConfiguration
            {
                BootstrapServers = string.Empty
            }
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoEndpoint_ExceptionThrown()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with { Endpoint = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoStrategy_ExceptionThrown()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with { Strategy = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static KafkaProducerConfiguration GetValidConfiguration() =>
        new()
        {
            Endpoint = new KafkaStaticProducerEndpointResolver("topic"),
            Client = new KafkaClientProducerConfiguration
            {
                BootstrapServers = "test-server"
            }
        };
}
