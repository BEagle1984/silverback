// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientProducerConfigurationBuilderTests
{
    [Fact]
    public void Build_ConfigurationReturned()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.WithBatchSize(100);

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.BatchSize.Should().Be(100);
    }

    [Fact]
    public void ThrowIfNotAcknowledged_ThrowIfNotAcknowledgedSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.ThrowIfNotAcknowledged();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.ThrowIfNotAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void IgnoreIfNotAcknowledged_ThrowIfNotAcknowledgedSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.IgnoreIfNotAcknowledged();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.ThrowIfNotAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void DisposeOnException_DisposeOnExceptionSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisposeOnException();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.DisposeOnException.Should().BeTrue();
    }

    [Fact]
    public void DisableDisposeOnException_DisposeOnExceptionSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisableDisposeOnException();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.DisposeOnException.Should().BeFalse();
    }

    [Fact]
    public void WithFlushTimeout_FlushTimeoutSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.WithFlushTimeout(TimeSpan.FromHours(42));

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.FlushTimeout.Should().Be(TimeSpan.FromHours(42));
    }

    [Fact]
    public void EnableDeliveryReports_EnableDeliveryReportsSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.EnableDeliveryReports();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableDeliveryReports.Should().BeTrue();
    }

    [Fact]
    public void DisableDeliveryReports_EnableDeliveryReportsSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisableDeliveryReports();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableDeliveryReports.Should().BeFalse();
    }

    [Fact]
    public void EnableIdempotence_EnableIdempotenceSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.EnableIdempotence();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableIdempotence.Should().BeTrue();
    }

    [Fact]
    public void DisableIdempotence_EnableIdempotenceSet()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisableIdempotence();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableIdempotence.Should().BeFalse();
    }
}
