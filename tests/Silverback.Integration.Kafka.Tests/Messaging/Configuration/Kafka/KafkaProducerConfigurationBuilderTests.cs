// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka
{
    public class KafkaProducerConfigurationBuilderTests
    {
        [Fact]
        public void Build_WithoutTopicName_ExceptionThrown()
        {
            KafkaProducerConfigurationBuilder<TestEventOne> builder = new(
                new KafkaClientConfiguration
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            Action act = () => builder.Build();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Build_WithoutBootstrapServer_ExceptionThrown()
        {
            KafkaProducerConfigurationBuilder<TestEventOne> builder = new();

            Action act = () =>
            {
                builder.ProduceTo("some-topic");
                builder.Build();
            };

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void ProduceTo_TopicName_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo("some-topic")
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaStaticProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().Be("some-topic");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", Partition.Any));
        }

        [Fact]
        public void ProduceTo_TopicNameAndPartition_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo("some-topic", 42)
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaStaticProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().Be("some-topic[42]");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", 42));
        }

        [Fact]
        public void ProduceTo_TopicPartition_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo(new TopicPartition("some-topic", 42))
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaStaticProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().Be("some-topic[42]");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", 42));
        }

        [Fact]
        public void ProduceTo_TopicNameAndPartitionFunction_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo("some-topic", _ => 42)
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().Be("some-topic");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", 42));
        }

        [Fact]
        public void ProduceTo_TopicNameFunction_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo(_ => "some-topic")
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().StartWith("dynamic-");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", Partition.Any));
        }

        [Fact]
        public void ProduceTo_TopicNameFunctionAndPartitionFunction_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo(_ => "some-topic", _ => 42)
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().StartWith("dynamic-");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", new Partition(42)));
        }

        [Fact]
        public void ProduceTo_TopicPartitionFunction_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo(_ => new TopicPartition("some-topic", 42))
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().StartWith("dynamic-");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", new Partition(42)));
        }

        [Fact]
        public void ProduceTo_TopicNameFormat_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo("some-topic-{0}", _ => new[] { "123" }, _ => 42)
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().Be("some-topic-{0}");
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
                null,
                endpointConfiguration,
                Substitute.For<IServiceProvider>());
            endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic-123", new Partition(42)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ProduceTo_WithInvalidTopicName_ExceptionThrown(string? topicName)
        {
            KafkaProducerConfigurationBuilder<TestEventOne> builder = new(
                new KafkaClientConfiguration
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            Action act = () => builder.ProduceTo(topicName!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ProduceTo_WithInvalidPartitionIndex_ExceptionThrown()
        {
            KafkaProducerConfigurationBuilder<TestEventOne> builder = new(
                new KafkaClientConfiguration
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            Action act = () => builder.ProduceTo("test", -42);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void UseEndpointResolver_EndpointSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .UseEndpointResolver<TestTypedEndpointResolver>()
                .Build();

            endpointConfiguration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver>();
            endpointConfiguration.RawName.Should().StartWith("dynamic-TestTypedEndpointResolver-");
        }

        [Fact]
        public void Configure_ConfigurationAction_ConfigurationSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo("some-topic")
                .ConfigureClient(
                    config =>
                    {
                        config.ThrowIfNotAcknowledged = false;
                        config.MessageTimeoutMs = 42;
                    })
                .Build();

            endpointConfiguration.Client.ThrowIfNotAcknowledged.Should().BeFalse();
            endpointConfiguration.Client.MessageTimeoutMs.Should().Be(42);
        }

        [Fact]
        public void Configure_WithBaseConfig_ConfigurationMerged()
        {
            KafkaClientConfiguration baseConfiguration = new()
            {
                BootstrapServers = "PLAINTEXT://tests",
                MessageMaxBytes = 42
            };
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(baseConfiguration)
                .ProduceTo("some-topic")
                .ConfigureClient(
                    config =>
                    {
                        config.ThrowIfNotAcknowledged = false;
                        config.MessageTimeoutMs = 42;
                        config.MessageMaxBytes = 4242;
                    }).Build();

            endpointConfiguration.Client.BootstrapServers.Should().Be("PLAINTEXT://tests");
            endpointConfiguration.Client.ThrowIfNotAcknowledged.Should().BeFalse();
            endpointConfiguration.Client.MessageTimeoutMs.Should().Be(42);
            endpointConfiguration.Client.MessageMaxBytes.Should().Be(4242);
            baseConfiguration.MessageMaxBytes.Should().Be(42);
        }

        [Fact]
        public void Configure_MultipleConfigurationActions_MergedConfigurationSet()
        {
            KafkaProducerConfiguration endpointConfiguration = new KafkaProducerConfigurationBuilder<TestEventOne>(
                    new KafkaClientConfiguration
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    })
                .ProduceTo("some-topic")
                .ConfigureClient(
                    config =>
                    {
                        config.ThrowIfNotAcknowledged = false;
                    })
                .ConfigureClient(
                    config =>
                    {
                        config.MessageTimeoutMs = 42;
                    })
                .Build();

            endpointConfiguration.Client.ThrowIfNotAcknowledged.Should().BeFalse();
            endpointConfiguration.Client.MessageTimeoutMs.Should().Be(42);
        }

        private sealed class TestTypedEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
        {
            public TopicPartition GetTopicPartition(TestEventOne? message) => new("some-topic", 42);
        }
    }
}
