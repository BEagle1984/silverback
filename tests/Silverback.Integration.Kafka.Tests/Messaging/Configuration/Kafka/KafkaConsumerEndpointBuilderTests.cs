// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka
{
    public class KafkaConsumerEndpointBuilderTests
    {
        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            Action act = () => builder.Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Build_WithoutBootstrapServer_ExceptionThrown()
        {
            var builder = new KafkaConsumerEndpointBuilder();

            Action act = () =>
            {
                builder.ConsumeFrom("topic");
                builder.Build();
            };

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ConsumeFrom_SingleTopicName_TopicSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ConsumeFrom("topic");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("topic");
            endpoint.Names.Should().BeEquivalentTo("topic");
            endpoint.TopicPartitions.Should().BeNull();
        }

        [Fact]
        public void ConsumeFrom_MultipleTopicNames_TopicsSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ConsumeFrom("topic1", "topic2");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("[topic1,topic2]");
            endpoint.Names.Should().BeEquivalentTo("topic1", "topic2");
            endpoint.TopicPartitions.Should().BeNull();
        }

        [Fact]
        public void ConsumeFrom_SingleTopicPartition_TopicSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ConsumeFrom(new TopicPartition("topic", 2));
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("topic[2]");
            endpoint.Names.Should().BeEquivalentTo("topic[2]");
            endpoint.TopicPartitions.Should().BeEquivalentTo(
                new TopicPartitionOffset("topic", 2, Offset.Unset));
        }

        [Fact]
        public void ConsumeFrom_MultipleTopicPartitions_TopicsSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ConsumeFrom(
                new TopicPartition("topic1", 0),
                new TopicPartition("topic1", 1),
                new TopicPartition("topic2", 2),
                new TopicPartition("topic2", 3));
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("[topic1[0],topic1[1],topic2[2],topic2[3]]");
            endpoint.Names.Should().BeEquivalentTo("topic1[0]", "topic1[1]", "topic2[2]", "topic2[3]");
            endpoint.TopicPartitions.Should().BeEquivalentTo(
                new TopicPartitionOffset("topic1", 0, Offset.Unset),
                new TopicPartitionOffset("topic1", 1, Offset.Unset),
                new TopicPartitionOffset("topic2", 2, Offset.Unset),
                new TopicPartitionOffset("topic2", 3, Offset.Unset));
        }

        [Fact]
        public void ConsumeFrom_MultipleTopicPartitionOffsets_TopicsSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ConsumeFrom(
                new TopicPartitionOffset("topic1", 0, Offset.Beginning),
                new TopicPartitionOffset("topic1", 1, Offset.End),
                new TopicPartitionOffset("topic2", 2, 42),
                new TopicPartitionOffset("topic2", 3, Offset.Unset));
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("[topic1[0],topic1[1],topic2[2],topic2[3]]");
            endpoint.Names.Should().BeEquivalentTo("topic1[0]", "topic1[1]", "topic2[2]", "topic2[3]");
            endpoint.TopicPartitions.Should().BeEquivalentTo(
                new TopicPartitionOffset("topic1", 0, Offset.Beginning),
                new TopicPartitionOffset("topic1", 1, Offset.End),
                new TopicPartitionOffset("topic2", 2, 42),
                new TopicPartitionOffset("topic2", 3, Offset.Unset));
        }

        [Fact]
        public void Configure_ConfigurationAction_ConfigurationSet()
        {
            var builder = new KafkaConsumerEndpointBuilder();

            builder
                .ConsumeFrom("topic")
                .Configure(
                    config =>
                    {
                        config.BootstrapServers = "PLAINTEXT://tests";
                        config.EnableAutoCommit = false;
                        config.CommitOffsetEach = 42;
                        config.GroupId = "group1";
                    });
            var endpoint = builder.Build();

            endpoint.Configuration.EnableAutoCommit.Should().Be(false);
            endpoint.Configuration.CommitOffsetEach.Should().Be(42);
            endpoint.Configuration.GroupId.Should().Be("group1");
        }

        [Fact]
        public void Configure_WithBaseConfig_ConfigurationMerged()
        {
            var baseConfig = new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests",
                MessageMaxBytes = 42
            };
            var builder = new KafkaConsumerEndpointBuilder(baseConfig);

            builder
                .ConsumeFrom("topic")
                .Configure(
                    config =>
                    {
                        config.EnableAutoCommit = false;
                        config.CommitOffsetEach = 42;
                        config.GroupId = "group1";
                        config.MessageMaxBytes = 4242;
                    });
            var endpoint = builder.Build();

            endpoint.Configuration.BootstrapServers.Should().Be("PLAINTEXT://tests");
            endpoint.Configuration.EnableAutoCommit.Should().Be(false);
            endpoint.Configuration.CommitOffsetEach.Should().Be(42);
            endpoint.Configuration.GroupId.Should().Be("group1");
            endpoint.Configuration.MessageMaxBytes.Should().Be(4242);
            baseConfig.MessageMaxBytes.Should().Be(42);
        }

        [Fact]
        public void ProcessPartitionsIndependently_ProcessPartitionsIndependentlySet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder
                .ConsumeFrom("topic")
                .ProcessPartitionsIndependently();
            var endpoint = builder.Build();

            endpoint.ProcessPartitionsIndependently.Should().BeTrue();
        }

        [Fact]
        public void ProcessAllPartitionsTogether_ProcessPartitionsIndependentlySet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder
                .ConsumeFrom("topic")
                .ProcessAllPartitionsTogether();
            var endpoint = builder.Build();

            endpoint.ProcessPartitionsIndependently.Should().BeFalse();
        }

        [Fact]
        public void LimitParallelism_MaxDegreeOfParallelismSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder
                .ConsumeFrom("topic")
                .LimitParallelism(42);
            var endpoint = builder.Build();

            endpoint.MaxDegreeOfParallelism.Should().Be(42);
        }

        [Fact]
        public void LimitBackpressure_BackpressureLimitSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder
                .ConsumeFrom("topic")
                .LimitBackpressure(42);
            var endpoint = builder.Build();

            endpoint.BackpressureLimit.Should().Be(42);
        }
    }
}
