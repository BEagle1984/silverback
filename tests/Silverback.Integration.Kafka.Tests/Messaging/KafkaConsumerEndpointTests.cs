// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging
{
    public class KafkaConsumerEndpointTests
    {
        [Fact]
        public void Constructor_SingleTopic_TopicSet()
        {
            var endpoint = new KafkaConsumerEndpoint("topic");

            endpoint.Name.Should().Be("topic");
            endpoint.Names.Should().BeEquivalentTo("topic");
            endpoint.TopicPartitions.Should().BeNull();
        }

        [Fact]
        public void Constructor_MultipleTopics_TopicsSet()
        {
            var endpoint = new KafkaConsumerEndpoint("topic1", "topic2");

            endpoint.Name.Should().Be("[topic1,topic2]");
            endpoint.Names.Should().BeEquivalentTo("topic1", "topic2");
            endpoint.TopicPartitions.Should().BeNull();
        }

        [Fact]
        public void Constructor_SingleTopicPartition_TopicsSet()
        {
            var endpoint = new KafkaConsumerEndpoint(new TopicPartition("topic", 2));

            endpoint.Name.Should().Be("topic[2]");
            endpoint.Names.Should().BeEquivalentTo("topic[2]");
            endpoint.TopicPartitions.Should().BeEquivalentTo(
                new TopicPartitionOffset("topic", 2, Offset.Unset));
        }

        [Fact]
        public void Constructor_MultipleTopicPartitions_TopicsSet()
        {
            var endpoint = new KafkaConsumerEndpoint(
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 1),
                new TopicPartition("topic2", 2),
                new TopicPartition("topic2", 3));

            endpoint.Name.Should().Be("[topic1[0],topic1[1],topic2[2],topic2[3]]");
            endpoint.Names.Should().BeEquivalentTo("topic1[0]", "topic1[1]", "topic2[2]", "topic2[3]");
            endpoint.TopicPartitions.Should().BeEquivalentTo(
                new TopicPartitionOffset("topic1", 0, Offset.Unset),
                new TopicPartitionOffset("topic1", 1, Offset.Unset),
                new TopicPartitionOffset("topic2", 2, Offset.Unset),
                new TopicPartitionOffset("topic2", 3, Offset.Unset));
        }

        [Fact]
        public void Constructor_TopicPartitionOffsets_TopicsSet()
        {
            var endpoint = new KafkaConsumerEndpoint(
                new TopicPartitionOffset("topic1", 0, Offset.Beginning),
                new TopicPartitionOffset("topic1", 1, Offset.End),
                new TopicPartitionOffset("topic2", 2, 42),
                new TopicPartitionOffset("topic2", 3, Offset.Unset));

            endpoint.Name.Should().Be("[topic1[0],topic1[1],topic2[2],topic2[3]]");
            endpoint.Names.Should().BeEquivalentTo("topic1[0]", "topic1[1]", "topic2[2]", "topic2[3]");
            endpoint.TopicPartitions.Should().BeEquivalentTo(
                new TopicPartitionOffset("topic1", 0, Offset.Beginning),
                new TopicPartitionOffset("topic1", 1, Offset.End),
                new TopicPartitionOffset("topic2", 2, 42),
                new TopicPartitionOffset("topic2", 3, Offset.Unset));
        }

        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new KafkaConsumerEndpoint("topic")
            {
                Configuration =
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new KafkaConsumerEndpoint("topic")
            {
                Configuration =
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var endpoint2 = new KafkaConsumerEndpoint("topic")
            {
                Configuration =
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentTopic_FalseIsReturned()
        {
            var endpoint1 = new KafkaConsumerEndpoint("topic")
            {
                Configuration =
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var endpoint2 = new KafkaConsumerEndpoint("topic2")
            {
                Configuration =
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new KafkaConsumerEndpoint("topic")
            {
                Configuration =
                {
                    AutoCommitIntervalMs = 1000
                }
            };

            var endpoint2 = new KafkaConsumerEndpoint("topic")
            {
                Configuration =
                {
                    BrokerAddressTtl = 2000
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Validate_ValidTopicAndConfiguration_NoExceptionThrown()
        {
            var endpoint = GetValidEndpoint();

            Action act = () => endpoint.Validate();

            act.Should().NotThrow<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingConfiguration_ExceptionThrown()
        {
            var endpoint = new KafkaConsumerEndpoint("topic")
            {
                Configuration = null!
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_InvalidConfiguration_ExceptionThrown()
        {
            var endpoint = new KafkaConsumerEndpoint("topic")
            {
                Configuration = new KafkaConsumerConfig()
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingTopic_ExceptionThrown()
        {
            var endpoint = new KafkaConsumerEndpoint(Array.Empty<string>())
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "test-server"
                }
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(42, true)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public void Validate_MaxDegreeOfParallelism_CorrectlyValidated(int value, bool isValid)
        {
            var endpoint = GetValidEndpoint();

            endpoint.MaxDegreeOfParallelism = value;

            Action act = () => endpoint.Validate();

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
            var endpoint = GetValidEndpoint();

            endpoint.BackpressureLimit = value;

            Action act = () => endpoint.Validate();

            if (isValid)
                act.Should().NotThrow();
            else
                act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        private static KafkaConsumerEndpoint GetValidEndpoint() =>
            new("test")
            {
                Configuration = new KafkaConsumerConfig
                {
                    BootstrapServers = "test-server"
                }
            };
    }
}
