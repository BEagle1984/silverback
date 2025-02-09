// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Serialization;

public class MockedSchemaRegistryClientFactoryFixture
{
    private readonly IConfluentSchemaRegistryClientFactory _clientFactory = new MockedConfluentSchemaRegistryClientFactory();

    [Fact]
    public void GetClient_ShouldReturnClient()
    {
        KafkaSchemaRegistryConfiguration configuration = GetValidConfiguration();

        ISchemaRegistryClient client = _clientFactory.GetClient(configuration);

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_ShouldReturnSameClient_WhenCalledTwiceWithSameConfiguration()
    {
        KafkaSchemaRegistryConfiguration configuration = GetValidConfiguration();

        ISchemaRegistryClient client1 = _clientFactory.GetClient(configuration);
        ISchemaRegistryClient client2 = _clientFactory.GetClient(configuration);

        client1.ShouldBeSameAs(client2);
    }

    [Fact]
    public void GetClient_ShouldReturnSameClient_WhenCalledWithEquivalentConfiguration()
    {
        KafkaSchemaRegistryConfiguration configuration1 = new()
        {
            Url = "http://localhost:8081"
        };
        KafkaSchemaRegistryConfiguration configuration2 = new()
        {
            Url = "http://localhost:8081"
        };

        ISchemaRegistryClient client1 = _clientFactory.GetClient(configuration1);
        ISchemaRegistryClient client2 = _clientFactory.GetClient(configuration2);

        client1.ShouldBeSameAs(client2);
    }

    [Fact]
    public void GetClient_ShouldReturnDifferentClient_WhenCalledWithDifferentConfiguration()
    {
        KafkaSchemaRegistryConfiguration configuration1 = new()
        {
            Url = "http://localhost:8081"
        };
        KafkaSchemaRegistryConfiguration configuration2 = new()
        {
            Url = "http://localhost:8082"
        };

        ISchemaRegistryClient client1 = _clientFactory.GetClient(configuration1);
        ISchemaRegistryClient client2 = _clientFactory.GetClient(configuration2);

        client1.ShouldNotBeSameAs(client2);
    }

    private static KafkaSchemaRegistryConfiguration GetValidConfiguration() =>
        new()
        {
            Url = "test-url"
        };
}
