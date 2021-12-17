// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerConfigurationBuilderTests
{
    // TODO: Test cloning of the wrapped dictionary

    [Fact]
    public void Build_WithoutTopic_ExceptionThrown()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        Action act = () => builder.Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Build_WithInvalidPartitionIndex_ExceptionThrown()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom("test", -42);

        Action act = () => builder.Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Build_WithoutBootstrapServer_ExceptionThrown()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();

        Action act = () =>
        {
            builder.ConsumeFrom("topic");
            builder.Build();
        };

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void ConsumeFrom_SingleTopicName_TopicPartitionsSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();
        builder.ConfigureClient(
            config => config
                .WithBootstrapServers("PLAINTEXT://whatever:1111")
                .WithGroupId("group1"));

        builder.ConsumeFrom("topic");
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic", Partition.Any, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_MultipleTopicNames_TopicPartitionsSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();
        builder.ConfigureClient(
            config => config
                .WithBootstrapServers("PLAINTEXT://whatever:1111")
                .WithGroupId("group1"));

        builder.ConsumeFrom("topic1", "topic2");
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic1", Partition.Any, Offset.Unset),
                new("topic2", Partition.Any, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_SingleTopicNameAndSinglePartition_TopicPartitionsSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom("topic", 1);
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic", 1, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_SingleTopicNameAndMultiplePartitions_TopicPartitionsSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom("topic", 1, 2, 3);
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new TopicPartitionOffset[]
            {
                new("topic", 1, Offset.Unset),
                new("topic", 2, Offset.Unset),
                new("topic", 3, Offset.Unset)
            });
    }

    [Fact]
    public void ConsumeFrom_MultipleTopicPartitions_TopicsSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom(
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic2", 2),
            new TopicPartition("topic2", 3));
        KafkaConsumerConfiguration configuration = builder.Build();

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
    public void ConsumeFrom_MultipleTopicPartitionOffsets_TopicPartitionsSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom(
            new TopicPartitionOffset("topic1", 0, Offset.Beginning),
            new TopicPartitionOffset("topic1", 1, Offset.End),
            new TopicPartitionOffset("topic2", 2, 42),
            new TopicPartitionOffset("topic2", 3, Offset.Unset));
        KafkaConsumerConfiguration configuration = builder.Build();

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
    public void ConsumeFrom_SingleTopicAndPartitionsProvider_TopicPartitionsAndProviderSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom(
            "topic1",
            partitions => partitions);
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>>();
    }

    [Fact]
    public void ConsumeFrom_MultipleTopicsAndPartitionsProvider_TopicPartitionsAndProviderSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        builder.ConsumeFrom(new[] { "topic1", "topic2" }, partitions => partitions);
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>>();
    }

    [Fact]
    public void ConsumeFrom_SingleTopicAndPartitionOffsetsProvide_TopicPartitionsAndResolverSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        KafkaConsumerConfiguration configuration = builder
            .ConsumeFrom(
                "topic1",
                partitions => partitions.Select(
                    partition =>
                        new TopicPartitionOffset(partition, Offset.Beginning))).Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>>();
    }

    [Fact]
    public void ConsumeFrom_MultipleTopicsAndPartitionOffsetsProvider_TopicPartitionsAndProviderSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

        KafkaConsumerConfiguration configuration = builder
            .ConsumeFrom(
                new[] { "topic1", "topic2" },
                topicPartitions => topicPartitions.Select(
                    partition =>
                        new TopicPartitionOffset(partition, Offset.Beginning)))
            .Build();

        configuration.TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
            });
        configuration.PartitionOffsetsProvider.Should().NotBeNull();
        configuration.PartitionOffsetsProvider.Should().BeOfType<Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>>();
    }

    [Fact]
    public void ConfigureClient_ConfigurationAction_ClientConfigurationSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();

        builder
            .ConsumeFrom("topic")
            .ConfigureClient(
                config => config with
                {
                    BootstrapServers = "PLAINTEXT://tests",
                    EnableAutoCommit = false,
                    CommitOffsetEach = 42,
                    GroupId = "group1"
                });
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.Client.EnableAutoCommit.Should().Be(false);
        configuration.Client.CommitOffsetEach.Should().Be(42);
        configuration.Client.GroupId.Should().Be("group1");
    }

    [Fact]
    public void ConfigureClient_WithBaseConfig_ClientonfigurationMerged()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "PLAINTEXT://tests",
            MessageMaxBytes = 42
        };
        KafkaConsumerConfigurationBuilder<object> builder = new(baseConfiguration);

        builder
            .ConsumeFrom("topic")
            .ConfigureClient(
                config => config with
                {
                    EnableAutoCommit = false,
                    CommitOffsetEach = 42,
                    GroupId = "group1",
                    MessageMaxBytes = 4242
                });
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.Client.BootstrapServers.Should().Be("PLAINTEXT://tests");
        configuration.Client.EnableAutoCommit.Should().Be(false);
        configuration.Client.CommitOffsetEach.Should().Be(42);
        configuration.Client.GroupId.Should().Be("group1");
        configuration.Client.MessageMaxBytes.Should().Be(4242);
        baseConfiguration.MessageMaxBytes.Should().Be(42);
    }

    [Fact]
    public void ConfigureClient_MultipleConfigurationActions_MergedClientConfigurationSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();

        builder
            .ConsumeFrom("topic")
            .ConfigureClient(
                config => config with
                {
                    BootstrapServers = "PLAINTEXT://tests",
                    EnableAutoCommit = false
                })
            .ConfigureClient(
                config => config with
                {
                    CommitOffsetEach = 42,
                    GroupId = "group1"
                });
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.Client.EnableAutoCommit.Should().Be(false);
        configuration.Client.CommitOffsetEach.Should().Be(42);
        configuration.Client.GroupId.Should().Be("group1");
    }

    [Fact]
    public void ConfigureClient_BuilderConfigurationAction_ClientConfigurationSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();

        builder
            .ConsumeFrom("topic")
            .ConfigureClient(
                config => config
                    .WithBootstrapServers("PLAINTEXT://tests")
                    .WithEnableAutoCommit(false)
                    .CommitOffsetEach(42)
                    .WithGroupId("group1"));
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.Client.EnableAutoCommit.Should().Be(false);
        configuration.Client.CommitOffsetEach.Should().Be(42);
        configuration.Client.GroupId.Should().Be("group1");
    }

    [Fact]
    public void ConfigureClient_BuilderWithBaseConfig_ClientonfigurationMerged()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "PLAINTEXT://tests",
            MessageMaxBytes = 42
        };
        KafkaConsumerConfigurationBuilder<object> builder = new(baseConfiguration);

        builder
            .ConsumeFrom("topic")
            .ConfigureClient(
                config => config
                    .WithEnableAutoCommit(false)
                    .CommitOffsetEach(42)
                    .WithGroupId("group1")
                    .WithMessageMaxBytes(4242));
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.Client.BootstrapServers.Should().Be("PLAINTEXT://tests");
        configuration.Client.EnableAutoCommit.Should().Be(false);
        configuration.Client.CommitOffsetEach.Should().Be(42);
        configuration.Client.GroupId.Should().Be("group1");
        configuration.Client.MessageMaxBytes.Should().Be(4242);
        baseConfiguration.MessageMaxBytes.Should().Be(42);
    }

    [Fact]
    public void ConfigureClient_BuilderMultipleConfigurationActions_MergedClientConfigurationSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();

        builder
            .ConsumeFrom("topic")
            .ConfigureClient(
                config => config
                    .WithBootstrapServers("PLAINTEXT://tests")
                    .CommitOffsetEach(42)
                    .WithEnableAutoCommit(false))
            .ConfigureClient(
                config => config
                    .WithGroupId("group1"));
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.Client.BootstrapServers.Should().Be("PLAINTEXT://tests");
        configuration.Client.EnableAutoCommit.Should().Be(false);
        configuration.Client.CommitOffsetEach.Should().Be(42);
        configuration.Client.GroupId.Should().Be("group1");
    }

    [Fact]
    public void ProcessPartitionsIndependently_ProcessPartitionsIndependentlySet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();
        builder.ConfigureClient(
            config => config
                .WithBootstrapServers("PLAINTEXT://whatever:1111")
                .WithGroupId("group1"));

        builder.ConsumeFrom("topic").ProcessPartitionsIndependently();
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.ProcessPartitionsIndependently.Should().BeTrue();
    }

    [Fact]
    public void ProcessAllPartitionsTogether_ProcessPartitionsIndependentlySet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();
        builder.ConfigureClient(
            config => config
                .WithBootstrapServers("PLAINTEXT://whatever:1111")
                .WithGroupId("group1"));

        builder.ConsumeFrom("topic").ProcessAllPartitionsTogether();
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.ProcessPartitionsIndependently.Should().BeFalse();
    }

    [Fact]
    public void LimitParallelism_MaxDegreeOfParallelismSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();
        builder.ConfigureClient(
            config => config
                .WithBootstrapServers("PLAINTEXT://whatever:1111")
                .WithGroupId("group1"));

        builder.ConsumeFrom("topic").LimitParallelism(42);
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.MaxDegreeOfParallelism.Should().Be(42);
    }

    [Fact]
    public void LimitBackpressure_BackpressureLimitSet()
    {
        KafkaConsumerConfigurationBuilder<object> builder = new();
        builder.ConfigureClient(
            config => config
                .WithBootstrapServers("PLAINTEXT://whatever:1111")
                .WithGroupId("group1"));

        builder.ConsumeFrom("topic").LimitBackpressure(42);
        KafkaConsumerConfiguration configuration = builder.Build();

        configuration.BackpressureLimit.Should().Be(42);
    }
}
