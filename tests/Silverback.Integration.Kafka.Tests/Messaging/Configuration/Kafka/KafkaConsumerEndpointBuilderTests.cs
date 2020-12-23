// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.KafkaEvents.Statistics;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka
{
    public class KafkaConsumerEndpointBuilderTests
    {
        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
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
                builder.ConsumeFrom("some-topic");
                builder.Build();
            };

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ConsumeFrom_SingleTopic_TopicNameSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

            builder.ConsumeFrom("some-topic");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("some-topic");
        }

        [Fact]
        public void ConsumeFrom_MultipleTopicsTopicNameSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

            builder.ConsumeFrom("some-topic", "some-other-topic");
            var endpoint = builder.Build();

            endpoint.Names.Should().BeEquivalentTo("some-topic", "some-other-topic");
        }

        [Fact]
        public void Configure_ConfigurationAction_ConfigurationSet()
        {
            var builder = new KafkaConsumerEndpointBuilder();

            builder
                .ConsumeFrom("some-topic")
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
                .ConsumeFrom("some-topic")
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
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

            builder
                .ConsumeFrom("some-topic")
                .ProcessPartitionsIndependently();
            var endpoint = builder.Build();

            endpoint.ProcessPartitionsIndependently.Should().BeTrue();
        }

        [Fact]
        public void ProcessAllPartitionsTogether_ProcessPartitionsIndependentlySet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

            builder
                .ConsumeFrom("some-topic")
                .ProcessAllPartitionsTogether();
            var endpoint = builder.Build();

            endpoint.ProcessPartitionsIndependently.Should().BeFalse();
        }

        [Fact]
        public void LimitParallelism_MaxDegreeOfParallelismSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

            builder
                .ConsumeFrom("some-topic")
                .LimitParallelism(42);
            var endpoint = builder.Build();

            endpoint.MaxDegreeOfParallelism.Should().Be(42);
        }

        [Fact]
        public void LimitBackpressure_BackpressureLimitSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });

            builder
                .ConsumeFrom("some-topic")
                .LimitBackpressure(42);
            var endpoint = builder.Build();

            endpoint.BackpressureLimit.Should().Be(42);
        }

        [Fact]
        public void OnKafkaError_Handler_HandlerSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });
            Func<Error, KafkaConsumer, bool> handler = (_, _) => true;

            builder
                .ConsumeFrom("some-topic")
                .OnKafkaError(handler);
            var endpoint = builder.Build();

            endpoint.Events.ErrorHandler.Should().BeSameAs(handler);
        }

        [Fact]
        public void OnOffsetsCommitted_Handler_HandlerSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });
            Action<CommittedOffsets, KafkaConsumer> handler = (_, _) => { };

            builder
                .ConsumeFrom("some-topic")
                .OnOffsetsCommitted(handler);
            var endpoint = builder.Build();

            endpoint.Events.OffsetsCommittedHandler.Should().BeSameAs(handler);
        }

        [Fact]
        public void OnPartitionsAssigned_Handler_HandlerSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });
            Func<IReadOnlyCollection<TopicPartition>, KafkaConsumer, IEnumerable<TopicPartitionOffset>> handler =
                (_, _) => null!;

            builder
                .ConsumeFrom("some-topic")
                .OnPartitionsAssigned(handler);
            var endpoint = builder.Build();

            endpoint.Events.PartitionsAssignedHandler.Should().BeSameAs(handler);
        }

        [Fact]
        public void OnPartitionsRevoked_Handler_HandlerSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });
            Action<IReadOnlyCollection<TopicPartitionOffset>, KafkaConsumer> handler = (_, _) => { };

            builder
                .ConsumeFrom("some-topic")
                .OnPartitionsRevoked(handler);
            var endpoint = builder.Build();

            endpoint.Events.PartitionsRevokedHandler.Should().BeSameAs(handler);
        }

        [Fact]
        public void OnStatisticsReceived_Handler_HandlerSet()
        {
            var builder = new KafkaConsumerEndpointBuilder(new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            });
            Action<KafkaStatistics, string, KafkaConsumer> handler = (_, _, _) => { };

            builder
                .ConsumeFrom("some-topic")
                .OnStatisticsReceived(handler);
            var endpoint = builder.Build();

            endpoint.Events.StatisticsHandler.Should().BeSameAs(handler);
        }
    }
}
