// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Dekaf.Messaging.Configuration.Kafka;

// Covers the validation rules specific to the Dekaf integration, the shared rules are covered by the tests of the
// Confluent.Kafka based integration
public class KafkaConsumerConfigurationTests
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Validate_ShouldThrow_WhenAutoCommitIntervalIsLessThanOne(int autoCommitIntervalMs)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { AutoCommitIntervalMs = autoCommitIntervalMs };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message.ShouldBe("AutoCommitIntervalMs must be greater or equal to 1.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(int.MaxValue)]
    public void Validate_ShouldNotThrow_WhenAutoCommitIntervalIsValid(int? autoCommitIntervalMs)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { AutoCommitIntervalMs = autoCommitIntervalMs };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenGroupInstanceIdIsSetWithoutGroupId()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = null,
            GroupInstanceId = "instance-1",
            CommitOffsets = false
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message.ShouldBe("The GroupId must be specified when the GroupInstanceId is set.");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenGroupInstanceIdIsSetWithGroupId()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { GroupInstanceId = "instance-1" };

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    private static KafkaConsumerConfiguration GetValidConfiguration() =>
        new()
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic", 42, Offset.Unset)
                    ])
                }
            ]),
            BootstrapServers = "PLAINTEXT://tests",
            GroupId = "group-42"
        };
}
