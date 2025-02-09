// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerConfigurationFixture
{
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

        configuration1.BootstrapServers.ShouldBe("config1");
        configuration2.BootstrapServers.ShouldBe("config2");

        configuration1.MessageCopyMaxBytes.ShouldBe(42);
        configuration2.MessageCopyMaxBytes.ShouldBe(42);
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

        configuration1.FlushTimeout.ShouldBe(TimeSpan.FromDays(42));
        configuration2.FlushTimeout.ShouldBe(TimeSpan.FromDays(42));
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

        configuration.AreDeliveryReportsEnabled.ShouldBe(expected);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsNull()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with { Endpoints = null! };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsEmpty()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>([])
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointResolverIsNotValid()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
            [
                new KafkaProducerEndpointConfiguration { EndpointResolver = null! }
            ])
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBootstrapServersIsNull()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            BootstrapServers = string.Empty
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenThrowIfNotAcknowledgedIsTrueButDeliveryReportIsDisabled()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            EnableDeliveryReports = false
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
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

        act.ShouldNotThrow();
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        KafkaProducerConfiguration configuration = GetValidConfiguration() with
        {
            BootstrapServers = "PLAINTEXT://tests",
            LingerMs = 42
        };

        ProducerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.BootstrapServers.ShouldBe("PLAINTEXT://tests");
        confluentConfig.LingerMs.ShouldBe(42);
    }

    [Fact]
    public void ToConfluentConfig_ShouldSetDefaultDeliveryReportFields()
    {
        KafkaProducerConfiguration configuration = new();

        ProducerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.DeliveryReportFields.ShouldBe("key,status");
    }

    [Fact]
    public void ToConfluentConfig_ShouldForceEnableBackgroundPoll()
    {
        KafkaProducerConfiguration configuration = new();

        ProducerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.EnableBackgroundPoll.ShouldBe(true);
    }

    private static KafkaProducerConfiguration GetValidConfiguration() => new()
    {
        BootstrapServers = "test-server",
        Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
        [
            new KafkaProducerEndpointConfiguration
            {
                EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
            }
        ])
    };
}
