// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        Action act = () => builder.Build();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromSingleTopicName()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom("topic");

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic", Partition.Any, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromMultipleTopicNames()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom("topic1", "topic2");

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic1", Partition.Any, Offset.Unset),
                new("topic2", Partition.Any, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromSingleTopicNameAndSinglePartition()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom("topic", 1);

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic", 1, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromSingleTopicNameAndMultiplePartitions()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom("topic", 1, 2, 3);

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic", 1, Offset.Unset),
                new("topic", 2, Offset.Unset),
                new("topic", 3, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromMultipleTopicPartitions()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom(
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic2", 2),
            new TopicPartition("topic2", 3));

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", 0, Offset.Unset),
                new TopicPartitionOffset("topic1", 1, Offset.Unset),
                new TopicPartitionOffset("topic2", 2, Offset.Unset),
                new TopicPartitionOffset("topic2", 3, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromMultipleTopicPartitionOffsets()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom(
            new TopicPartitionOffset("topic1", 0, Offset.Beginning),
            new TopicPartitionOffset("topic1", 1, Offset.End),
            new TopicPartitionOffset("topic2", 2, 42),
            new TopicPartitionOffset("topic2", 3, Offset.Unset));

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", 0, Offset.Beginning),
                new TopicPartitionOffset("topic1", 1, Offset.End),
                new TopicPartitionOffset("topic2", 2, 42),
                new TopicPartitionOffset("topic2", 3, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromSingleTopicAndPartitionsProvider()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom("topic1", partitions => partitions);

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>>>();
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromMultipleTopicsAndPartitionsProvider()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder.ConsumeFrom(new[] { "topic1", "topic2" }, partitions => partitions);

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>>>();
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromSingleTopicAndPartitionOffsetsProvider()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder
            .ConsumeFrom(
                "topic1",
                partitions => partitions.Select(
                    partition =>
                        new TopicPartitionOffset(partition, Offset.Beginning)));

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>>>();
    }

    [Fact]
    public void ConsumeFrom_ShouldSetTopicPartitionsFromMultipleTopicsAndPartitionOffsetsProvider()
    {
        KafkaConsumerEndpointConfigurationBuilder<object> builder = new();

        builder
            .ConsumeFrom(
                new[] { "topic1", "topic2" },
                topicPartitions => topicPartitions.Select(
                    partition =>
                        new TopicPartitionOffset(partition, Offset.Beginning)));

        KafkaConsumerEndpointConfiguration configuration = builder.Build();
        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>>>();
    }
}
