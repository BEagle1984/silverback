// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentAdminClientFactoryExtensionsFixture
{
    private readonly ConfluentAdminClientFactory _factory = new(Substitute.For<ISilverbackLogger<ConfluentAdminClientFactory>>());

    [Fact]
    public void GetClient_ShouldReturnClientForBuilderAction()
    {
        IAdminClient client = _factory.GetClient(builder => builder.WithBootstrapServers("PLAINTEXT://test:9092"));

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldReturnClientForClientConfiguration()
    {
        IAdminClient client = _factory.GetClient(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldReturnClientForProducerConfiguration()
    {
        IAdminClient client = _factory.GetClient(
            new KafkaProducerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldIgnoreProducerSpecificConfigurations()
    {
        IAdminClient client = _factory.GetClient(
            new KafkaProducerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                EnableDeliveryReports = false
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldReturnClientForConsumerConfiguration()
    {
        IAdminClient client = _factory.GetClient(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldIgnoreConsumerSpecificConfigurations()
    {
        IAdminClient client = _factory.GetClient(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                ConsumeResultFields = "something"
            });

        client.ShouldNotBeNull();
    }
}
