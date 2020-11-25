// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.KafkaEvents.Statistics;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration
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
        public void ProduceTo_TopicName_TopicNameSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });

            builder.ProduceTo("some-topic");
            var endpoint = builder.Build();

            endpoint.Name.Should().Be("some-topic");
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
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests",
                    MessageMaxBytes = 42
                });

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
        }

        [Fact]
        public void OnStatisticsReceived_Handler_HandlerSet()
        {
            var builder = new KafkaProducerEndpointBuilder(
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://tests"
                });
            Action<KafkaStatistics, string, KafkaProducer> handler = (_, _, _) => { };

            builder
                .ProduceTo("some-topic")
                .OnStatisticsReceived(handler);
            var endpoint = builder.Build();

            endpoint.Events.StatisticsHandler.Should().BeSameAs(handler);
        }
    }
}
