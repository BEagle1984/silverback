// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Collections;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaConsumerConfigurationTests
{
    [Fact]
    public void IsStaticAssignment_WithSubscription_FalseReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
                }),
            PartitionOffsetsProvider = null
        };

        configuration.IsStaticAssignment.Should().BeFalse();
    }

    [Fact]
    public void IsStaticAssignment_WithStaticPartitionAssignment_TrueReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 1, Offset.Unset),
                    new TopicPartitionOffset("topic", 2, Offset.Unset)
                }),
            PartitionOffsetsProvider = null
        };

        configuration.IsStaticAssignment.Should().BeTrue();
    }

    [Fact]
    public void IsStaticAssignment_WithPartitionOffsetsProvider_TrueReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
                }),
            PartitionOffsetsProvider = partitions =>
                partitions.Take(3).Select(partition => new TopicPartitionOffset(partition, Offset.Unset))
        };

        configuration.IsStaticAssignment.Should().BeTrue();
    }

    [Fact]
    public void RawName_SubscriptionToSingleTopic_TopicNameReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
                }),
            PartitionOffsetsProvider = null
        };

        configuration.RawName.Should().Be("topic");
    }

    [Fact]
    public void RawName_SubscriptionToMultipleTopics_TopicNamesReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic3", Partition.Any, Offset.Unset)
                }),
            PartitionOffsetsProvider = null
        };

        configuration.RawName.Should().Be("topic1,topic2,topic3");
    }

    [Fact]
    public void RawName_StaticPartitionsAssignment_TopicNamesAndPartitionsReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", 3, Offset.Unset),
                    new TopicPartitionOffset("topic1", 4, Offset.Unset),
                    new TopicPartitionOffset("topic2", 1, Offset.Unset)
                }),
            PartitionOffsetsProvider = null
        };

        configuration.RawName.Should().Be("topic1[3],topic1[4],topic2[1]");
    }

    [Theory]
    [InlineData(true, 100)]
    [InlineData(false, 1)]
    public void ProcessPartitionsIndependently_DefaultMaxDegreeOfParallelismSetAccordingly(
        bool processPartitionsIndependently,
        int expectedMaxDegreeOfParallelism)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            ProcessPartitionsIndependently = processPartitionsIndependently
        };

        configuration.MaxDegreeOfParallelism.Should().Be(expectedMaxDegreeOfParallelism);
    }

    [Fact]
    public void Equals_SameEndpointInstance_TrueReturned()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration();

        configuration.Equals(configuration).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameConfiguration_TrueReturned()
    {
        KafkaConsumerConfiguration configuration1 = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 42, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };
        KafkaConsumerConfiguration configuration2 = new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 42, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };

        configuration1.Equals(configuration2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTopic_FalseReturned()
    {
        KafkaConsumerConfiguration configuration1 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };
        KafkaConsumerConfiguration configuration2 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentPartition_FalseReturned()
    {
        KafkaConsumerConfiguration configuration1 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 1, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };
        KafkaConsumerConfiguration configuration2 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 2, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MultipleDifferentTopics_FalseReturned()
    {
        KafkaConsumerConfiguration configuration1 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };
        KafkaConsumerConfiguration configuration2 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic3", Partition.Any, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentConfiguration_FalseReturned()
    {
        KafkaConsumerConfiguration configuration1 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 1, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 1000
            }
        };
        KafkaConsumerConfiguration configuration2 = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 2, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                AutoCommitIntervalMs = 2000
            }
        };

        configuration1.Equals(configuration2).Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidEndpoint_NoExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration();

        Action act = () => configuration.Validate();

        act.Should().NotThrow<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MissingConfiguration_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Client = null!
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
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
    public void Validate_MaxDegreeOfParallelism_CorrectlyValidated(
        int value,
        bool processPartitionsIndependently,
        bool isValid)
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
            act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void Validate_BackpressureLimit_CorrectlyValidated(int value, bool isValid)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            BackpressureLimit = value
        };

        Action act = () => configuration.Validate();

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_InvalidConfiguration_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            Client = new KafkaClientConsumerConfiguration()
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MissingTopic_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(Array.Empty<TopicPartitionOffset>())
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_InvalidTopicName_ExceptionThrown(string? topicName)
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset(topicName, Partition.Any, Offset.Unset)
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_InvalidPartition_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic3", -42, Offset.Unset)
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_SpecifiedBothPartitionAndPartitionOffsetsProvider_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 3, Offset.Unset)
                }),
            PartitionOffsetsProvider = _ => Array.Empty<TopicPartitionOffset>()
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_DuplicatedPartition_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", 1, Offset.Unset),
                    new TopicPartitionOffset("topic1", 1, Offset.Beginning)
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_MultipleUniquePartitions_NoExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", 1, Offset.Unset),
                    new TopicPartitionOffset("topic1", 2, Offset.Unset),
                    new TopicPartitionOffset("topic1", 3, Offset.Unset),
                    new TopicPartitionOffset("topic2", 1, Offset.Unset),
                    new TopicPartitionOffset("topic2", 2, Offset.Unset)
                })
        };

        Action act = () => configuration.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MixedSpecifiedAndAnyPartitions_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic1", 1, Offset.Unset),
                    new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset)
                })
        };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoSequence_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { Sequence = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Validate_NoErrorPolicy_ExceptionThrown()
    {
        KafkaConsumerConfiguration configuration = GetValidConfiguration() with { ErrorPolicy = null! };

        Action act = () => configuration.Validate();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    private static KafkaConsumerConfiguration GetValidConfiguration() =>
        new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                new[]
                {
                    new TopicPartitionOffset("topic", 42, Offset.Unset)
                }),
            Client = new KafkaClientConsumerConfiguration
            {
                BootstrapServers = "test-server"
            }
        };
}
