// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka
{
    public class KafkaProducerEndpointBuilderTests
    {
        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            var builder = new KafkaProducerEndpointBuilder(
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
            var builder = new KafkaProducerEndpointBuilder();

            Action act = () =>
            {
                builder.ProduceTo("some-topic");
                builder.Build();
            };

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ProduceTo_TopicName_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo("some-topic");
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TopicNameAndPartition_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo("some-topic", 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void ProduceTo_TopicNameFunction_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo(_ => "some-topic");
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TypedTopicNameFunction_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo<TestEventOne>(_ => "some-topic");
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TopicNameAndPartitionFunctions_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo(_ => "some-topic", _ => 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void ProduceTo_TypedTopicNameAndPartitionFunctions_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo<TestEventOne>(_ => "some-topic", _ => 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void ProduceTo_TopicNameFunctionWithServiceProvider_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo((_, _) => "some-topic");
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TypedTopicNameFunctionWithServiceProvider_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo<TestEventOne>((_, _) => "some-topic");
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TopicNameAndPartitionFunctionsWithServiceProvider_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo((_, _) => "some-topic", (_, _) => 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void ProduceTo_TypedTopicNameAndPartitionFunctionsWithServiceProvider_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo<TestEventOne>((_, _) => "some-topic", (_, _) => 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void ProduceTo_TopicNameFormat_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo("some-topic-{0}", _ => new[] { "123" });
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic-123");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TypedTopicNameFormat_TopicSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo<TestEventOne>("some-topic-{0}", _ => new[] { "123" });
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic-123");
            endpoint.GetPartition(null!, null!).Should().Be(Partition.Any);
        }

        [Fact]
        public void ProduceTo_TopicNameFormatAndPartitionFunction_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo("some-topic-{0}", _ => new[] { "123" }, _ => 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic-123");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void ProduceTo_TypedTopicNameFormatAndPartitionFunction_TopicAndPartitionSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo<TestEventOne>("some-topic-{0}", _ => new[] { "123" }, _ => 42);
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, null!).Should().Be("some-topic-123");
            endpoint.GetPartition(null!, null!).Should().Be(new Partition(42));
        }

        [Fact]
        public void UseEndpointNameResolver_TopicAndPartitionSet()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(TestEndpointNameResolver))
                .Returns(new TestEndpointNameResolver());

            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.UseEndpointNameResolver<TestEndpointNameResolver>();
            var endpoint = builder.Build();

            endpoint.GetActualName(null!, serviceProvider).Should().Be("some-topic");
            endpoint.GetPartition(null!, serviceProvider).Should().Be(new Partition(42));
        }

        [Fact]
        public void Configure_ConfigurationAction_ConfigurationSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder
                .ProduceTo("some-topic")
                .Configure(
                    config =>
                    {
                        config.ThrowIfNotAcknowledged = false;
                        config.MessageTimeoutMs = 42;
                    });
            var endpoint = builder.Build();

            endpoint.Configuration.ThrowIfNotAcknowledged.Should().BeFalse();
            endpoint.Configuration.MessageTimeoutMs.Should().Be(42);
        }

        [Fact]
        public void Configure_WithBaseConfig_ConfigurationMerged()
        {
            var baseConfig = new KafkaClientConfig
            {
                BootstrapServers = "PLAINTEXT://tests",
                MessageMaxBytes = 42
            };
            var builder = new KafkaProducerEndpointBuilder(baseConfig);

            builder
                .ProduceTo("some-topic")
                .Configure(
                    config =>
                    {
                        config.ThrowIfNotAcknowledged = false;
                        config.MessageTimeoutMs = 42;
                        config.MessageMaxBytes = 4242;
                    });
            var endpoint = builder.Build();

            endpoint.Configuration.BootstrapServers.Should().Be("PLAINTEXT://tests");
            endpoint.Configuration.ThrowIfNotAcknowledged.Should().BeFalse();
            endpoint.Configuration.MessageTimeoutMs.Should().Be(42);
            endpoint.Configuration.MessageMaxBytes.Should().Be(4242);
            baseConfig.MessageMaxBytes.Should().Be(42);
        }

        private class TestEndpointNameResolver : IKafkaProducerEndpointNameResolver
        {
            public string GetName(IOutboundEnvelope envelope) => "some-topic";

            public int? GetPartition(IOutboundEnvelope envelope) => 42;
        }
    }
}
