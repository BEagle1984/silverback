// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Collections;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Sequences.Batch;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerConfigurationFixture
{
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

    [Theory]
    [InlineData(true, true)]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void IsAutoCommitEnabled_ShouldReturnCorrectValue(bool? enableAutoCommit, bool expected)
    {
        KafkaConsumerConfiguration configuration = new()
        {
            EnableAutoCommit = enableAutoCommit
        };

        configuration.IsAutoCommitEnabled.Should().Be(expected);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsNull()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { Endpoints = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointsIsEmpty()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(Array.Empty<KafkaConsumerEndpointConfiguration>())
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("At least one endpoint must be configured.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenEndpointIsNotValid()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset(string.Empty, -42, Offset.Unset)
                            })
                    }
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMixingSubscriptionsWithStaticAssignments()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", 1, Offset.Unset)
                            })
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                            })
                    }
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Cannot mix static partition assignments and subscriptions*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenMixingConstantStaticAssignmentsAndPartitionOffsetProvider()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", 1, Offset.Unset)
                            })
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                            }),
                        PartitionOffsetsProvider = _ => ValueTask.FromResult(Enumerable.Empty<TopicPartitionOffset>())
                    }
                })
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTopicIsSpecifiedMoreThanOnce()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", 1, Offset.Unset)
                            })
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", 2, Offset.Unset)
                            })
                    }
                })
        };
        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>()
            .WithMessage("Cannot connect to the same topic in different endpoints*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenSubscribingWithNoGroupId(string? groupId)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = groupId,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                            })
                    }
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenStaticAssignmentWithoutGroupId()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            GroupId = null,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", 1, Offset.Unset)
                            })
                    }
                })
        };

        Action act = () => configuration.Validate();

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

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Theory]
    [InlineData(true, null, true)]
    [InlineData(false, 1, true)]
    [InlineData(false, 42, true)]
    [InlineData(true, 1, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, 42, false)]
    [InlineData(false, null, false)]
    [InlineData(false, 0, false)]
    [InlineData(false, -1, false)]
    public void Validate_ShouldValidateCommitSettings(bool enableAutoCommit, int? commitOffsetEach, bool isValid)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            EnableAutoCommit = enableAutoCommit,
            CommitOffsetEach = commitOffsetEach
        };

        Action act = () => configuration.Validate();

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

        Action act = () => configuration.Validate();

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().ThrowExactly<BrokerConfigurationException>();
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

        Action act = () => configuration.Validate();

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProcessingPartitionsTogetherWithBatchAndNoBatch()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = false,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                            }),
                        Batch = new BatchSettings
                        {
                            Size = 42
                        }
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                            })
                    }
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("All endpoints must use the same Batch settings*");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenProcessingPartitionsTogetherWithDifferentBatchSettings()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = false,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                            }),
                        Batch = new BatchSettings
                        {
                            Size = 42
                        }
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                            }),
                        Batch = new BatchSettings
                        {
                            Size = 31337
                        }
                    }
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<BrokerConfigurationException>().WithMessage("All endpoints must use the same Batch settings*");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenProcessingPartitionsIndependentlyWithDifferentBatchSettings()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = true,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                            }),
                        Batch = new BatchSettings
                        {
                            Size = 42
                        }
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                            }),
                        Batch = new BatchSettings
                        {
                            Size = 31337
                        }
                    },
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic3", Partition.Any, Offset.Unset)
                            })
                    }
                })
        };

        Action act = () => configuration.Validate();

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
                new[]
                {
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                            new[]
                            {
                                new TopicPartitionOffset("topic", 42, Offset.Unset)
                            })
                    }
                }),
            BootstrapServers = "PLAINTEXT://tests",
            GroupId = "group-42"
        };
}
