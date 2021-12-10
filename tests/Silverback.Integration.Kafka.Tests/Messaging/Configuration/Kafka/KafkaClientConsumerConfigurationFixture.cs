// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConsumerConfigurationFixture
{
    [Fact]
    public void Constructor_ShouldCloneBaseConfiguration()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "base",
            ClientId = "client"
        };

        KafkaClientConsumerConfiguration configuration1 = new(baseConfiguration)
        {
            BootstrapServers = "config1"
        };

        KafkaClientConsumerConfiguration configuration2 = new(baseConfiguration)
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
        KafkaClientConsumerConfiguration configuration1 = new()
        {
            BootstrapServers = "config1",
            GroupId = "group"
        };

        KafkaClientConsumerConfiguration configuration2 = configuration1 with
        {
            BootstrapServers = "config2"
        };

        configuration1.BootstrapServers.Should().Be("config1");
        configuration2.BootstrapServers.Should().Be("config2");

        configuration1.GroupId.Should().Be("group");
        configuration2.GroupId.Should().Be("group");
    }

    [Fact]
    public void CloneConstructor_ShouldCloneCustomProperties()
    {
        KafkaClientConsumerConfiguration configuration1 = new()
        {
            CommitOffsetEach = 42
        };

        KafkaClientConsumerConfiguration configuration2 = configuration1 with
        {
        };

        configuration1.CommitOffsetEach.Should().Be(42);
        configuration2.CommitOffsetEach.Should().Be(42);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void IsAutoCommitEnabled_ShouldReturnCorrectValue(bool? enableAutoCommit, bool expected)
    {
        KafkaClientConsumerConfiguration configuration = new()
        {
            EnableAutoCommit = enableAutoCommit
        };

        configuration.IsAutoCommitEnabled.Should().Be(expected);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenConfigurationIsValid()
    {
        KafkaClientConsumerConfiguration configuration = GetValidConfig();

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMissingBootstrapServers()
    {
        KafkaClientConsumerConfiguration configuration = GetValidConfig() with
        {
            BootstrapServers = string.Empty
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBothEnableAutoCommitIsTrueAndCommitOffsetEachIsSet()
    {
        KafkaClientConsumerConfiguration configuration = GetValidConfig() with
        {
            EnableAutoCommit = true,
            CommitOffsetEach = 10
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenNeitherEnableAutoCommitIsTrueOrCommitOffsetEachIsSet()
    {
        KafkaClientConsumerConfiguration configuration = GetValidConfig() with
        {
            EnableAutoCommit = false,
            CommitOffsetEach = 0
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static KafkaClientConsumerConfiguration GetValidConfig() => new()
    {
        BootstrapServers = "test-server"
    };
}
