// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConfigurationBuilderTests
{
    [Fact]
    public void Build_ConfigurationReturned()
    {
        KafkaClientConfigurationBuilder builder = new();
        builder.WithBootstrapServers("tests:9092");

        KafkaClientConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.BootstrapServers.Should().Be("tests:9092");
    }
}
