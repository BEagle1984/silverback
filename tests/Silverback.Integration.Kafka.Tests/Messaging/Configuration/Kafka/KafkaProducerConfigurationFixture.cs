// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerConfigurationFixture
{
    [Fact]
    public void Constructor_ShouldSetDefaultDeliveryReportFields()
    {
        KafkaProducerConfiguration configuration = new();

        configuration.GetConfluentClientConfig().DeliveryReportFields.Should().Be("key,status");
    }

    [Fact]
    public void Constructor_ShouldForceEnableBackgroundPoll()
    {
        KafkaProducerConfiguration configuration = new();

        configuration.GetConfluentClientConfig().EnableBackgroundPoll.Should().BeTrue();
    }

    [Fact]
    public void CloneConstructor_ShouldCloneWrappedClientConfig()
    {
        KafkaProducerConfiguration configuration1 = new()
        {
            BootstrapServers = "config1",
            MessageCopyMaxBytes = 42
        };

        KafkaProducerConfiguration configuration2 = configuration1 with
        {
            BootstrapServers = "config2"
        };

        configuration1.BootstrapServers.Should().Be("config1");
        configuration2.BootstrapServers.Should().Be("config2");

        configuration1.MessageCopyMaxBytes.Should().Be(42);
        configuration2.MessageCopyMaxBytes.Should().Be(42);
    }

    [Fact]
    public void CloneConstructor_ShouldCloneCustomProperties()
    {
        KafkaProducerConfiguration configuration1 = new()
        {
            FlushTimeout = TimeSpan.FromDays(42)
        };

        KafkaProducerConfiguration configuration2 = configuration1 with
        {
        };

        configuration1.FlushTimeout.Should().Be(TimeSpan.FromDays(42));
        configuration2.FlushTimeout.Should().Be(TimeSpan.FromDays(42));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void AreDeliveryReportsEnabled_ShouldReturnCorrectValue(bool? enableDeliveryReports, bool expected)
    {
        KafkaProducerConfiguration configuration = new()
        {
            EnableDeliveryReports = enableDeliveryReports
        };

        configuration.AreDeliveryReportsEnabled.Should().Be(expected);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsNull()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with { Endpoints = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsEmpty()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>([])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNotValid()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
            [
                new KafkaProducerEndpointConfiguration { Endpoint = null! }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBootstrapServersIsNull()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            BootstrapServers = string.Empty
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenThrowIfNotAcknowledgedIsTrueButDeliveryReportIsDisabled()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            EnableDeliveryReports = false
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(null)]
    public void Validate_ShouldNotThrow_WhenThrowIfNotAcknowledgedIsTrueAndDeliveryReportIsEnabled(bool? enableDeliveryReports)
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            EnableDeliveryReports = enableDeliveryReports
        };

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void GetConfluentClientConfig_ShouldReturnClientConfig()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            BootstrapServers = "PLAINTEXT://tests",
            LingerMs = 42
        };

        ProducerConfig clientConfig = configuration.GetConfluentClientConfig();

        clientConfig.BootstrapServers.Should().Be("PLAINTEXT://tests");
        clientConfig.LingerMs.Should().Be(42);
    }

    private static KafkaProducerConfiguration GetValidConfiguration() => new()
    {
        BootstrapServers = "test-server",
        Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
        [
            new KafkaProducerEndpointConfiguration
            {
                Endpoint = new KafkaStaticProducerEndpointResolver("topic1")
            }
        ])
    };
}
