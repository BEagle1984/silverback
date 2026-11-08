// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentAdminClientFactoryTests
{
    private readonly ProducerCollection _producers = [];

    private readonly ConsumerCollection _consumers = [];

    private readonly ConfluentAdminClientFactory _factory;

    public ConfluentAdminClientFactoryTests()
    {
        _factory = new ConfluentAdminClientFactory(
            _producers,
            _consumers,
            Substitute.For<ISilverbackLogger<ConfluentAdminClientFactory>>());
    }

    [Fact]
    public void GetClient_ShouldReturnClientForBuilderAction()
    {
        using IAdminClient client = _factory.GetClient(builder => builder.WithBootstrapServers("PLAINTEXT://test:9092"));

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldReturnClientForClientConfiguration()
    {
        using IAdminClient client = _factory.GetClient(
            new KafkaClientConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldReturnClientForProducerConfiguration()
    {
        using IAdminClient client = _factory.GetClient(
            new KafkaProducerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldIgnoreProducerSpecificConfigurations()
    {
        using IAdminClient client = _factory.GetClient(
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
        using IAdminClient client = _factory.GetClient(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_WithConfiguration_ShouldIgnoreConsumerSpecificConfigurations()
    {
        using IAdminClient client = _factory.GetClient(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                ConsumeResultFields = "something"
            });

        client.ShouldNotBeNull();
    }

    [Fact]
    public void GetClient_ShouldReturnClientForKafkaProducer()
    {
        KafkaProducer producer = new(
            "producer1",
            Substitute.For<IConfluentProducerWrapper>(),
            GetProducerConfiguration("client1", new KafkaStaticProducerEndpointResolver("topic1")),
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());

        using IAdminClient client = _factory.GetClient(producer);

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClient_ShouldReturnClientForKafkaTransactionalProducer()
    {
        KafkaTransactionalProducer producer = GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic1"));

        using IAdminClient client = _factory.GetClient(producer);

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClient_ShouldThrow_WhenNotKafkaProducer()
    {
        Action act = () => _factory.GetClient(Substitute.For<IProducer>());

        Exception exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The producer is not a Kafka producer.");
    }

    [Fact]
    public void GetClient_ShouldReturnClientForKafkaConsumer()
    {
        using IAdminClient client = _factory.GetClient(GetConsumer("client1"));

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClient_ShouldThrow_WhenNotKafkaConsumer()
    {
        Action act = () => _factory.GetClient(Substitute.For<IConsumer>());

        Exception exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The consumer is not a Kafka consumer.");
    }

    [Fact]
    public void GetClientForProducer_ShouldReturnClientForKafkaProducer()
    {
        SetProducers(GetKafkaProducer("producer", "client1"));

        using IAdminClient client = _factory.GetClientForProducer("producer");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForProducer_ShouldNotMatchGeneratedName()
    {
        SetProducers(GetKafkaProducer("orders", "client1"), GetKafkaProducer("orders-1", "client2"));

        using IAdminClient client = _factory.GetClientForProducer("orders-1");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForProducer_ShouldReturnClientForKafkaTransactionalProducer()
    {
        SetProducers(
            new KafkaTransactionalProducer(
                "producer",
                GetProducerConfiguration("client1", new KafkaStaticProducerEndpointResolver("topic1")),
                Substitute.For<IKafkaTransactionalProducerCollection>()));

        using IAdminClient client = _factory.GetClientForProducer("producer");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForProducer_ShouldIgnoreOtherBrokersProducers()
    {
        IProducer otherBrokerProducer = Substitute.For<IProducer>();
        otherBrokerProducer.Name.Returns("producer");
        otherBrokerProducer.EndpointConfiguration.Returns(
            new KafkaProducerEndpointConfiguration
            {
                EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
            });
        SetProducers(otherBrokerProducer);

        Action act = () => _factory.GetClientForProducer("producer");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka producer has been configured with name 'producer'.");
    }

    [Fact]
    public void GetClientForProducer_ShouldThrow_WhenNameIsNull()
    {
        Action act = () => _factory.GetClientForProducer(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void GetClientForConsumer_ShouldReturnClientForMatchingConsumer()
    {
        SetConsumers(GetConsumer("client1"), GetConsumer("client2"));

        using IAdminClient client = _factory.GetClientForConsumer("client2");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumer_ShouldThrow_WhenConsumerNotFound()
    {
        SetConsumers(GetConsumer("client1"));

        Action act = () => _factory.GetClientForConsumer("client2");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka consumer has been configured with name 'client2'.");
    }

    [Fact]
    public void GetClientForConsumer_ShouldThrow_WhenNameIsNull()
    {
        Action act = () => _factory.GetClientForConsumer(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData("friendly")]
    [InlineData("topic2")]
    [InlineData("topic2[1]")]
    public void GetClientForProducerEndpoint_ShouldReturnClientForMatchingProducer(string endpointName)
    {
        SetProducers(
            GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic1")),
            GetProducer("client2", new KafkaStaticProducerEndpointResolver("topic2", 1), "friendly"));

        using IAdminClient client = _factory.GetClientForProducerEndpoint(endpointName);

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldPreferFriendlyName_WhenMatchingBothFriendlyAndTopicName()
    {
        SetProducers(
            GetProducer("client1", new KafkaStaticProducerEndpointResolver("name")),
            GetProducer("client2", new KafkaStaticProducerEndpointResolver("topic"), "name"));

        using IAdminClient client = _factory.GetClientForProducerEndpoint("name");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldIgnoreOtherBrokersProducers()
    {
        IProducer otherBrokerProducer = Substitute.For<IProducer>();
        otherBrokerProducer.EndpointConfiguration.Returns(
            new KafkaProducerEndpointConfiguration
            {
                EndpointResolver = new KafkaStaticProducerEndpointResolver("topic")
            });
        SetProducers(otherBrokerProducer, GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic")));

        using IAdminClient client = _factory.GetClientForProducerEndpoint("topic");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldThrow_WhenEndpointNotFound()
    {
        SetProducers(GetProducer("client1", new KafkaStaticProducerEndpointResolver("topic1")));

        Action act = () => _factory.GetClientForProducerEndpoint("topic2");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka producer has been configured for endpoint 'topic2'.");
    }

    [Fact]
    public void GetClientForProducerEndpoint_ShouldThrow_WhenEndpointNameIsNull()
    {
        Action act = () => _factory.GetClientForProducerEndpoint(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Theory]
    [InlineData("friendly")]
    [InlineData("topic2")]
    [InlineData("topic3")]
    [InlineData("topic2,topic3[1]")]
    public void GetClientForConsumerEndpoint_ShouldReturnClientForMatchingConsumer(string endpointName)
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("topic1")),
            GetConsumer(
                "client2",
                GetConsumerEndpoint("other"),
                GetConsumerEndpoint(
                    "friendly",
                    new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset),
                    new TopicPartitionOffset("topic3", 1, Offset.Unset))));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint(endpointName);

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldPreferFriendlyName_WhenMatchingBothFriendlyAndTopicName()
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("name")),
            GetConsumer(
                "client2",
                GetConsumerEndpoint("name", new TopicPartitionOffset("topic", Partition.Any, Offset.Unset))));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("name");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldReturnClientForConsumerSubscribingMatchingPattern()
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("topic1")),
            GetConsumer("client2", GetConsumerEndpoint("^orders-[a-z]+$")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("orders-eu");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldRequirePatternToMatchEntireName_WhenUsingConsumerProtocol()
    {
        IKafkaConsumer prefixConsumer = GetConsumer("client1", GetConsumerEndpoint("^orders"));
        prefixConsumer.Configuration.Returns(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                ClientId = "client1",
                GroupProtocol = GroupProtocol.Consumer
            });
        SetConsumers(prefixConsumer, GetConsumer("client2", GetConsumerEndpoint("^orders-.*")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("orders-eu");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldThrow_WhenEndpointNameIsPatternMatchingItself()
    {
        SetConsumers(GetConsumer("client1", GetConsumerEndpoint("other", new TopicPartitionOffset("^.*", Partition.Any, Offset.Unset))));

        Action act = () => _factory.GetClientForConsumerEndpoint("^.+");

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldPreferTopicName_WhenMatchingBothTopicNameAndPattern()
    {
        SetConsumers(
            GetConsumer("client1", GetConsumerEndpoint("^orders-")),
            GetConsumer("client2", GetConsumerEndpoint("orders-eu")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("orders-eu");

        client.Name.ShouldStartWith("client2");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldIgnoreOtherBrokersConsumers()
    {
        IConsumer otherBrokerConsumer = Substitute.For<IConsumer>();
        otherBrokerConsumer.EndpointsConfiguration.Returns([GetConsumerEndpoint("topic")]);
        SetConsumers(otherBrokerConsumer, GetConsumer("client1", GetConsumerEndpoint("topic")));

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("topic");

        client.Name.ShouldStartWith("client1");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldThrow_WhenEndpointNotFound()
    {
        SetConsumers(GetConsumer("client1", GetConsumerEndpoint("topic1")));

        Action act = () => _factory.GetClientForConsumerEndpoint("topic2");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No Kafka consumer has been configured for endpoint 'topic2'.");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldThrow_WhenEndpointNameIsNull()
    {
        Action act = () => _factory.GetClientForConsumerEndpoint(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    private static KafkaTransactionalProducer GetProducer(
        string clientId,
        KafkaStaticProducerEndpointResolver endpointResolver,
        string? friendlyName = null) =>
        new(
            "producer",
            GetProducerConfiguration(clientId, endpointResolver, friendlyName),
            Substitute.For<IKafkaTransactionalProducerCollection>());

    private static KafkaProducer GetKafkaProducer(string addedName, string clientId)
    {
        IConfluentProducerWrapper wrapper = Substitute.For<IConfluentProducerWrapper>();
        wrapper.Name.Returns(addedName);

        return new KafkaProducer(
            $"{addedName}-1",
            wrapper,
            GetProducerConfiguration(clientId, new KafkaStaticProducerEndpointResolver($"topic-{clientId}")),
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());
    }

    private static KafkaProducerConfiguration GetProducerConfiguration(
        string clientId,
        KafkaStaticProducerEndpointResolver endpointResolver,
        string? friendlyName = null) =>
        new()
        {
            BootstrapServers = "PLAINTEXT://test:9092",
            ClientId = clientId,
            Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
            [
                new KafkaProducerEndpointConfiguration
                {
                    EndpointResolver = endpointResolver,
                    FriendlyName = friendlyName
                }
            ])
        };

    private static IKafkaConsumer GetConsumer(string clientId, params KafkaConsumerEndpointConfiguration[] endpoints)
    {
        IKafkaConsumer consumer = Substitute.For<IKafkaConsumer>();
        consumer.Name.Returns(clientId);
        consumer.Configuration.Returns(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = "PLAINTEXT://test:9092",
                ClientId = clientId
            });
        consumer.EndpointsConfiguration.Returns(endpoints);

        return consumer;
    }

    private static KafkaConsumerEndpointConfiguration GetConsumerEndpoint(string topic) =>
        new()
        {
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
            [
                new TopicPartitionOffset(topic, Partition.Any, Offset.Unset)
            ])
        };

    private static KafkaConsumerEndpointConfiguration GetConsumerEndpoint(string friendlyName, params TopicPartitionOffset[] topicPartitions) =>
        new()
        {
            FriendlyName = friendlyName,
            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(topicPartitions)
        };

    private void SetProducers(params IProducer[] producers)
    {
        foreach (IProducer producer in producers)
        {
            _producers.Add(producer);
        }
    }

    private void SetConsumers(params IConsumer[] consumers)
    {
        foreach (IConsumer consumer in consumers)
        {
            _consumers.Add(consumer);
        }
    }
}
