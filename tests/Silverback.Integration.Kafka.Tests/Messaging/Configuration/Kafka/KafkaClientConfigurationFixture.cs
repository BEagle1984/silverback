// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConfigurationFixture
{
    [Fact]
    public void Constructor_ShouldCloneBaseConfiguration()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "base",
            ClientId = "client"
        };

        KafkaClientConfiguration configuration1 = new(baseConfiguration)
        {
            BootstrapServers = "config1"
        };

        KafkaClientConfiguration configuration2 = new(baseConfiguration)
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
    public void CloneConstructor_ShouldCloneWrappedClientConfig()
    {
        KafkaClientConfiguration configuration1 = new()
        {
            BootstrapServers = "config1",
            ClientId = "client"
        };

        KafkaClientConfiguration configuration2 = configuration1 with
        {
            BootstrapServers = "config2"
        };

        configuration1.BootstrapServers.Should().Be("config1");
        configuration2.BootstrapServers.Should().Be("config2");

        configuration1.ClientId.Should().Be("client");
        configuration2.ClientId.Should().Be("client");
    }
}
