// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConsumerConfigurationTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void IsAutoCommitEnabled_CorrectlySet(bool? enableAutoCommit, bool expected)
    {
        KafkaClientConsumerConfiguration config = new()
        {
            EnableAutoCommit = enableAutoCommit
        };

        config.IsAutoCommitEnabled.Should().Be(expected);
    }

    [Fact]
    public void Validate_ValidConfiguration_NoExceptionThrown()
    {
        KafkaClientConsumerConfiguration config = GetValidConfig();

        Action act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MissingBootstrapServers_ExceptionThrown()
    {
        KafkaClientConsumerConfiguration config = GetValidConfig() with
        {
            BootstrapServers = string.Empty
        };

        Action act = () => config.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_AutoCommitWithCommitOffsetEach_ExceptionThrown()
    {
        KafkaClientConsumerConfiguration config = GetValidConfig() with
        {
            EnableAutoCommit = true,
            CommitOffsetEach = 10
        };

        Action act = () => config.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoAutoCommitAndNoCommitOffsetEach_ExceptionThrown()
    {
        KafkaClientConsumerConfiguration config = GetValidConfig() with
        {
            EnableAutoCommit = false,
            CommitOffsetEach = 0
        };

        Action act = () => config.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static KafkaClientConsumerConfiguration GetValidConfig() => new()
    {
        BootstrapServers = "test-server"
    };
}
