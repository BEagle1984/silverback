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
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Sequences.Batch;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerConfigurationFixture
{
    [Fact]
    public void Constructor_ShouldSetEnableAutoCommit()
    {
        KafkaConsumerConfiguration configuration = new();

        configuration.EnableAutoCommit.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldSetGroupIdToUnset()
    {
        KafkaConsumerConfiguration configuration = new();

        configuration.GroupId.Should().Be("not-set");
    }

    [Fact]
    public void Constructor_ShouldDisableAutoOffsetStore()
    {
        KafkaConsumerConfiguration configuration = new();

        configuration.GetConfluentClientConfig().EnableAutoOffsetStore.Should().BeFalse();
    }

    [Fact]
    public void CloneConstructor_ShouldCloneWrappedClientConfig()
    {
        KafkaConsumerConfiguration configuration1 = new()
        {
            BootstrapServers = "config1",
            GroupId = "group"
        };

        KafkaConsumerConfiguration configuration2 = configuration1 with
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
        KafkaConsumerConfiguration configuration1 = new()
        {
            CommitOffsetEach = 42
        };

        KafkaConsumerConfiguration configuration2 = configuration1 with
        {
        };

        configuration1.CommitOffsetEach.Should().Be(42);
        configuration2.CommitOffsetEach.Should().Be(42);
    }

    [Fact]
    public void GroupId_ShouldSet()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = "group1"
        };

        configuration.GroupId.Should().Be("group1");
    }

    [InlineData(null)]
    [InlineData("")]
    [Theory]
    public void GroupId_ShouldReturnUnset_WhenNullOrEmpty(string? groupId)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = groupId!
        };

        configuration.GroupId.Should().Be(KafkaConsumerConfiguration.UnsetGroupId);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsNull()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { Endpoints = null! };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsEmpty()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>([])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("At least one endpoint must be configured.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNotValid()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset(string.Empty, -42, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMixingSubscriptionsWithStaticAssignments()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 1, Offset.Unset)
                    ])
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Cannot mix static partition assignments and subscriptions*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenMixingConstantStaticAssignmentsAndPartitionOffsetProvider()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 1, Offset.Unset)
                    ])
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                    ]),
                    PartitionOffsetsProvider = _ => ValueTask.FromResult(Enumerable.Empty<TopicPartitionOffset>())
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicIsSpecifiedMoreThanOnce()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 1, Offset.Unset)
                    ])
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 2, Offset.Unset)
                    ])
                }
            ])
        };
        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Cannot connect to the same topic in different endpoints*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenSubscribingWithoutGroupId(string? groupId)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = groupId!,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("The GroupId must be specified when the partitions are assigned dynamically. *");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenCommittingWithNoGroupId(string? groupId)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = groupId!,
            CommitOffsets = true,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 1, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("The GroupId should be specified when committing the offsets to the broker. *");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenSpecifyingOffsetStoreWithNoGroupId(string? groupId)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = groupId!,
            CommitOffsets = false,
            ClientSideOffsetStore = new InMemoryKafkaOffsetStoreSettings(),
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 1, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("The GroupId should be specified when using a client side offset store.");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenStaticAssignmentAndNotCommittingWithoutGroupId()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = null!,
            CommitOffsets = false,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", 1, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldThrow_WhenBootstrapServersIsNull(string? bootstrapServers)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            BootstrapServers = bootstrapServers
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Theory]
    [InlineData(true, true, null, true)]
    [InlineData(true, false, 1, true)]
    [InlineData(true, false, 42, true)]
    [InlineData(true, true, 1, false)]
    [InlineData(true, true, 0, false)]
    [InlineData(true, true, 42, false)]
    [InlineData(true, false, null, false)]
    [InlineData(true, false, 0, false)]
    [InlineData(true, false, -1, false)]
    [InlineData(false, false, 42, false)]
    public void Validate_ShouldValidateCommitSettings(bool commitOffsets, bool enableAutoCommit, int? commitOffsetEach, bool isValid)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            CommitOffsets = commitOffsets,
            EnableAutoCommit = enableAutoCommit,
            CommitOffsetEach = commitOffsetEach
        };

        Action act = configuration.Validate;

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(1, false, true)]
    [InlineData(42, true, true)]
    [InlineData(42, false, false)]
    [InlineData(0, true, false)]
    [InlineData(0, false, false)]
    [InlineData(-1, true, false)]
    [InlineData(-1, false, false)]
    public void Validate_ShouldValidateMaxDegreeOfParallelism(int value, bool processPartitionsIndependently, bool isValid)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = processPartitionsIndependently,
            MaxDegreeOfParallelism = value
        };

        Action act = configuration.Validate;

        if (isValid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().ThrowExactly<BrokerConfigurationException>()
                .WithMessage(
                    value < 1
                        ? "MaxDegreeOfParallelism must be greater or equal to 1."
                        : "MaxDegreeOfParallelism cannot be greater than 1 when the partitions aren't processed independently.");
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void Validate_ShouldValidateBackpressureLimit(int value, bool isValid)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            BackpressureLimit = value
        };

        Action act = configuration.Validate;

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("The backpressure limit must be greater or equal to 1.");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void Validate_ShouldValidateGetMetadataTimeout(int valueInSeconds, bool isValid)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GetMetadataTimeout = TimeSpan.FromSeconds(valueInSeconds)
        };

        Action act = configuration.Validate;

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("The get metadata timeout must be greater than 0.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProcessingPartitionsTogetherWithBatchAndNoBatch()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = false,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                    ]),
                    Batch = new BatchSettings
                    {
                        Size = 42
                    }
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("All endpoints must use the same Batch settings*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProcessingPartitionsTogetherWithDifferentBatchSettings()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = false,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                    ]),
                    Batch = new BatchSettings
                    {
                        Size = 42
                    }
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                    ]),
                    Batch = new BatchSettings
                    {
                        Size = 31337
                    }
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("All endpoints must use the same Batch settings*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenProcessingPartitionsIndependentlyWithDifferentBatchSettings()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = true,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                    ]),
                    Batch = new BatchSettings
                    {
                        Size = 42
                    }
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                    ]),
                    Batch = new BatchSettings
                    {
                        Size = 31337
                    }
                },
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic3", Partition.Any, Offset.Unset)
                    ])
                }
            ])
        };

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void GetConfluentClientConfig_ShouldReturnClientConfig()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            BootstrapServers = "PLAINTEXT://tests",
            GroupId = "group-42",
            FetchMaxBytes = 42
        };

        ConsumerConfig clientConfig = configuration.GetConfluentClientConfig();

        clientConfig.BootstrapServers.Should().Be("PLAINTEXT://tests");
        clientConfig.GroupId.Should().Be("group-42");
        clientConfig.FetchMaxBytes.Should().Be(42);
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
