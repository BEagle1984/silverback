// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerEndpointConfigurationFixture
{
    [Fact]
    public void IsStaticAssignment_ShouldBeFalse_WhenSubscribedToAnyPartition()
    {
        KafkaConsumerEndpointConfiguration configuration = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
            ]),
            PartitionOffsetsProvider = null
        };

        configuration.IsStaticAssignment.Should().BeFalse();
    }

    [Fact]
    public void IsStaticAssignment_ShouldBeTrue_WhenSubscribedToSpecificPartitions()
    {
        KafkaConsumerEndpointConfiguration configuration = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic", 1, Offset.Unset),
                new TopicPartitionOffset("topic", 2, Offset.Unset)
            ]),
            PartitionOffsetsProvider = null
        };

        configuration.IsStaticAssignment.Should().BeTrue();
    }

    [Fact]
    public void IsStaticAssignment_ShouldBeTrue_WhenPartitionOffsetsProviderIsSet()
    {
        KafkaConsumerEndpointConfiguration configuration = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
            ]),
            PartitionOffsetsProvider = _ => ValueTask.FromResult(Enumerable.Empty<TopicPartitionOffset>())
        };

        configuration.IsStaticAssignment.Should().BeTrue();
    }

    [Fact]
    public void RawName_ShouldReturnTopicName_WhenSubscribedToSingleTopic()
    {
        KafkaConsumerEndpointConfiguration configuration = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
            ]),
            PartitionOffsetsProvider = null
        };

        configuration.RawName.Should().Be("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicNames_WhenSubscribedToMultipleTopics()
    {
        KafkaConsumerEndpointConfiguration configuration = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic3", Partition.Any, Offset.Unset)
            ]),
            PartitionOffsetsProvider = null
        };
        configuration.RawName.Should().Be("topic1,topic2,topic3");
    }

    [Fact]
    public void RawName_ShouldReturnTopicNamesAndPartitions_WhenSubscribedToStaticPartitions()
    {
        KafkaConsumerEndpointConfiguration configuration = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic1", 3, Offset.Unset),
                new TopicPartitionOffset("topic1", 4, Offset.Unset),
                new TopicPartitionOffset("topic2", 1, Offset.Unset)
            ]),
            PartitionOffsetsProvider = null
        };

        configuration.RawName.Should().Be("topic1[3],topic1[4],topic2[1]");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration();
        Action act = configuration.Validate;
        act.Should().NotThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicPartitionsIsNull()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with { TopicPartitions = null! };
        Action act = configuration.Validate;
        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicPartitionsIsEmpty()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>([])
        };
        Action act = configuration.Validate;
        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenTopicNameIsNotValid(string? topicName)
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset(topicName, Partition.Any, Offset.Unset)
            ])
        };
        Action act = configuration.Validate;
        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenPartitionIsNotInvalid()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic3", -42, Offset.Unset)
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBothPartitionAndPartitionOffsetsProviderAreSpecified()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic", 3, Offset.Unset)
            ]),
            PartitionOffsetsProvider = _ => ValueTask.FromResult(Enumerable.Empty<TopicPartitionOffset>())
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenPartitionIsDuplicated()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic1", 1, Offset.Unset),
                new TopicPartitionOffset("topic1", 1, Offset.Beginning)
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMixingSubscriptionsAndStaticAssignments()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic1", 1, Offset.Unset),
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
            ])
        };
        Action act = configuration.Validate;
        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Cannot mix Partition.Any with a specific partition assignment*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenMultipleUniquePartitionsAreSpecified()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic1", 1, Offset.Unset),
                new TopicPartitionOffset("topic1", 2, Offset.Unset),
                new TopicPartitionOffset("topic1", 3, Offset.Unset),
                new TopicPartitionOffset("topic2", 1, Offset.Unset),
                new TopicPartitionOffset("topic2", 2, Offset.Unset)
            ])
        };
        Action act = configuration.Validate;
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDeserializerIsNull()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Deserializer = null!
        };
        Action act = configuration.Validate;
        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSequenceIsNull()
    {
        KafkaConsumerEndpointConfiguration configuration = GetValidConfiguration() with
        {
            Sequence = null!
        };
        Action act = configuration.Validate;
        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    private static KafkaConsumerEndpointConfiguration GetValidConfiguration() =>
        new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset("topic", 42, Offset.Unset)
            ])
        };
}
