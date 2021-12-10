// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConfigurationBuilderFixture
{
    [Fact]
    public void Constructor_ShouldCloneBaseConfiguration()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "base",
            ClientId = "client"
        };

        KafkaClientConfigurationBuilder builder1 = new(baseConfiguration);
        builder1.WithBootstrapServers("builder1");
        KafkaClientConfigurationBuilder builder2 = new(baseConfiguration);
        builder2.WithBootstrapServers("builder2");

        KafkaClientConfiguration configuration1 = builder1.Build();
        KafkaClientConfiguration configuration2 = builder2.Build();

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
        KafkaClientConfigurationBuilder builder = new();
        builder.WithBootstrapServers("tests:9092");

        KafkaClientConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.BootstrapServers.Should().Be("tests:9092");
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaClientConfigurationBuilder builder = new();

        builder.WithBootstrapServers("one");
        KafkaClientConfiguration configuration1 = builder.Build();

        builder.WithBootstrapServers("two");
        KafkaClientConfiguration configuration2 = builder.Build();

        configuration1.BootstrapServers.Should().Be("one");
        configuration2.BootstrapServers.Should().Be("two");
    }
}
