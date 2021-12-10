// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientProducerConfigurationFixture
{
    [Fact]
    public void Constructor_ShouldCloneBaseConfiguration()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "base",
            ClientId = "client"
        };

        KafkaClientProducerConfiguration configuration1 = new(baseConfiguration)
        {
            BootstrapServers = "config1"
        };

        KafkaClientProducerConfiguration configuration2 = new(baseConfiguration)
        {
            BootstrapServers = "config2"
        };

        baseConfiguration.BootstrapServers.Should().Be("base");
        configuration1.BootstrapServers.Should().Be("config1");
        configuration2.BootstrapServers.Should().Be("config2");

        baseConfiguration.ClientId.Should().Be("client");
        configuration1.ClientId.Should().Be("client");
        configuration2.ClientId.Should().Be("client");
    }

    [Fact]
    public void Constructor_ShouldSetDefaultDeliveryReportFields()
    {
        KafkaClientProducerConfiguration configuration = new();

        configuration.DeliveryReportFields.Should().Be("key,status");
    }

    [Fact]
    public void CloneConstructor_ShouldCloneWrappedClientConfig()
    {
        KafkaClientProducerConfiguration configuration1 = new()
        {
            BootstrapServers = "config1",
            MessageCopyMaxBytes = 42
        };

        KafkaClientProducerConfiguration configuration2 = configuration1 with
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
        KafkaClientProducerConfiguration configuration1 = new()
        {
            FlushTimeout = TimeSpan.FromDays(42)
        };

        KafkaClientProducerConfiguration configuration2 = configuration1 with
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
        KafkaClientProducerConfiguration configuration = new()
        {
            EnableDeliveryReports = enableDeliveryReports
        };

        configuration.AreDeliveryReportsEnabled.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "all", true)]
    [InlineData(true, "some,fields,and,status", true)]
    [InlineData(null, "all", true)]
    [InlineData(null, "some,fields,and,status", true)]
    [InlineData(true, "some,fields", false)]
    [InlineData(false, "all", false)]
    [InlineData(false, "some,fields,and,status", false)]
    public void ArePersistenceStatusReportsEnabled_ShouldReturnCorrectValue(
        bool? enableDeliveryReports,
        string deliveryReportFields,
        bool expected)
    {
        KafkaClientProducerConfiguration configuration = new()
        {
            EnableDeliveryReports = enableDeliveryReports,
            DeliveryReportFields = deliveryReportFields
        };

        configuration.ArePersistenceStatusReportsEnabled.Should().Be(expected);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenConfigurationIsValid()
    {
        KafkaClientProducerConfiguration configuration = GetValidConfig();

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMissingBootstrapServers()
    {
        KafkaClientProducerConfiguration configuration = GetValidConfig() with
        {
            BootstrapServers = string.Empty
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Theory]
    [InlineData(true, "some,fields")]
    [InlineData(null, "some,fields")]
    [InlineData(false, "all")]
    [InlineData(false, "status")]
    public void Validate_ShouldThrow_WhenThrowIfNotAcknowledgedIsTrueButDeliveryReportIsDisabled(
        bool? enableDeliveryReports,
        string deliveryReportFields)
    {
        KafkaClientProducerConfiguration configuration = GetValidConfig() with
        {
            EnableDeliveryReports = enableDeliveryReports,
            DeliveryReportFields = deliveryReportFields
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(true, "all")]
    [InlineData(true, "status")]
    [InlineData(null, "")]
    [InlineData(null, "all")]
    [InlineData(null, "status")]
    public void Validate_ShouldNotThrow_WhenThrowIfNotAcknowledgedIsTrueAndDeliveryReportIsEnabled(
        bool? enableDeliveryReports,
        string deliveryReportFields)
    {
        KafkaClientProducerConfiguration configuration = GetValidConfig() with
        {
            EnableDeliveryReports = enableDeliveryReports,
            DeliveryReportFields = deliveryReportFields
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    private static KafkaClientProducerConfiguration GetValidConfig() => new()
    {
        BootstrapServers = "test-server"
    };
}
