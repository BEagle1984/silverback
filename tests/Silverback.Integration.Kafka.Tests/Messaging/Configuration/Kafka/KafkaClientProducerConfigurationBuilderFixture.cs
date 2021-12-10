// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientProducerConfigurationBuilderFixture
{
    [Fact]
    public void Constructor_ShouldCloneBaseConfiguration()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "base",
            ClientId = "client"
        };

        KafkaClientProducerConfigurationBuilder builder1 = new(baseConfiguration);
        builder1.WithBootstrapServers("builder1");
        KafkaClientProducerConfigurationBuilder builder2 = new(baseConfiguration);
        builder2.WithBootstrapServers("builder2");

        KafkaClientProducerConfiguration configuration1 = builder1.Build();
        KafkaClientProducerConfiguration configuration2 = builder2.Build();

        baseConfiguration.BootstrapServers.Should().Be("base");
        configuration1.BootstrapServers.Should().Be("builder1");
        configuration2.BootstrapServers.Should().Be("builder2");

        baseConfiguration.ClientId.Should().Be("client");
        configuration1.ClientId.Should().Be("client");
        configuration2.ClientId.Should().Be("client");
    }

    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.WithBatchSize(100);

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.BatchSize.Should().Be(100);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaClientProducerConfigurationBuilder builder = new();

        builder.WithBootstrapServers("one");
        KafkaClientProducerConfiguration configuration1 = builder.Build();

        builder.WithBootstrapServers("two");
        KafkaClientProducerConfiguration configuration2 = builder.Build();

        configuration1.BootstrapServers.Should().Be("one");
        configuration2.BootstrapServers.Should().Be("two");
    }

    [Fact]
    public void ThrowIfNotAcknowledged_ShouldSetThrowIfNotAcknowledged()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.ThrowIfNotAcknowledged();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.ThrowIfNotAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void IgnoreIfNotAcknowledged_ShouldSetThrowIfNotAcknowledged()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.IgnoreIfNotAcknowledged();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.ThrowIfNotAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void DisposeOnException_ShouldSetDisposeOnException()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisposeOnException();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.DisposeOnException.Should().BeTrue();
    }

    [Fact]
    public void DisableDisposeOnException_ShouldSetDisposeOnException()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisableDisposeOnException();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.DisposeOnException.Should().BeFalse();
    }

    [Fact]
    public void WithFlushTimeout_ShouldSetFlushTimeout()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.WithFlushTimeout(TimeSpan.FromHours(42));

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.FlushTimeout.Should().Be(TimeSpan.FromHours(42));
    }

    [Fact]
    public void EnableDeliveryReports_ShouldSetEnableDeliveryReports()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.EnableDeliveryReports();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableDeliveryReports.Should().BeTrue();
    }

    [Fact]
    public void DisableDeliveryReports_ShouldSetEnableDeliveryReports()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisableDeliveryReports();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableDeliveryReports.Should().BeFalse();
    }

    [Fact]
    public void EnableIdempotence_ShouldSetEnableIdempotence()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.EnableIdempotence();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableIdempotence.Should().BeTrue();
    }

    [Fact]
    public void DisableIdempotence_ShouldSetEnableIdempotence()
    {
        KafkaClientProducerConfigurationBuilder builder = new();
        builder.DisableIdempotence();

        KafkaClientProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableIdempotence.Should().BeFalse();
    }
}
