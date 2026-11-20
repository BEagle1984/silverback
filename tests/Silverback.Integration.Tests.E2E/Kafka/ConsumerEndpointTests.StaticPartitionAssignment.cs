// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointTests
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromStaticPartitionAssignment()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(
                        new TopicPartition("topic1", 2),
                        new TopicPartition("topic2", 2)))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producerTopic1Partition1 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 1)));
        IProducer producerTopic1Partition2 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 2)));
        IProducer producerTopic2Partition1 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 1)));
        IProducer producerTopic2Partition2 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 2)));

        await producerTopic1Partition1.ProduceAsync(new TestEventOne());
        await producerTopic1Partition2.ProduceAsync(new TestEventOne());
        await producerTopic2Partition1.ProduceAsync(new TestEventOne());
        await producerTopic2Partition2.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromStaticPartitionAssignment_WhenUsingResolver()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(
                        DefaultTopicName,
                        partitions => partitions.Skip(1)))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producerPartition0 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 0)));
        IProducer producerPartition1 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 1)));
        IProducer producerPartition2 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 2)));

        await producerPartition0.ProduceAsync(new TestEventOne());
        await producerPartition1.ProduceAsync(new TestEventOne());
        await producerPartition2.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromStaticPartitionAssignment_WhenUsingResolverForMultipleTopics()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(
                        ["topic1", "topic2"],
                        _ =>
                        [
                            new TopicPartition("topic1", 2),
                            new TopicPartition("topic2", 1)
                        ]))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producerTopic1Partition1 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 1)));
        IProducer producerTopic1Partition2 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 2)));
        IProducer producerTopic2Partition1 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 1)));
        IProducer producerTopic2Partition2 = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 2)));

        await producerTopic1Partition1.ProduceAsync(new TestEventOne());
        await producerTopic1Partition2.ProduceAsync(new TestEventOne());
        await producerTopic2Partition1.ProduceAsync(new TestEventOne());
        await producerTopic2Partition2.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeAllPartitionsFromBeginning()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume(endpoint => endpoint.ConsumeFromBeginning("topic1", "topic2"))))
            .AddIntegrationSpyAndSubscriber());

        await ProduceToEachPartitionAsync("topic1", "topic2");

        await ConsumeTwiceAsync("consumer");

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(12);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeSpecifiedPartitionsFromBeginning()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume(endpoint => endpoint.ConsumeFromBeginning(DefaultTopicName, 0, 2))))
            .AddIntegrationSpyAndSubscriber());

        await ProduceToEachPartitionAsync(DefaultTopicName);

        await ConsumeTwiceAsync("consumer");

        Helper.Spy.InboundEnvelopes.Select(envelope => ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition.Partition.Value)
            .ShouldBe([0, 2, 0, 2], ignoreOrder: true);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeSpecifiedTopicPartitionsFromBeginning()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer",
                    consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume(endpoint => endpoint.ConsumeFromBeginning(
                            new TopicPartition("topic1", 2),
                            new TopicPartition("topic2", 1)))))
            .AddIntegrationSpyAndSubscriber());

        await ProduceToEachPartitionAsync("topic1", "topic2");

        await ConsumeTwiceAsync("consumer");

        Helper.Spy.InboundEnvelopes.Select(envelope => ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition)
            .ShouldBe(
                [
                    new TopicPartition("topic1", 2),
                    new TopicPartition("topic2", 1),
                    new TopicPartition("topic1", 2),
                    new TopicPartition("topic2", 1)
                ],
                ignoreOrder: true);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeResolvedPartitionsFromBeginning()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(
                    "consumer1",
                    consumer => consumer
                        .WithGroupId("group1")
                        .Consume(endpoint => endpoint.ConsumeFromBeginning(
                            "topic1",
                            partitions => partitions.Where(partition => partition.Partition == 0))))
                .AddConsumer(
                    "consumer2",
                    consumer => consumer
                        .WithGroupId("group2")
                        .Consume(endpoint => endpoint.ConsumeFromBeginning(
                            ["topic2", "topic3"],
                            partitions => partitions.Where(partition => partition.Partition == 1)))))
            .AddIntegrationSpyAndSubscriber());

        await ProduceToEachPartitionAsync("topic1", "topic2", "topic3");

        await ConsumeTwiceAsync("consumer1", "consumer2");

        Helper.Spy.InboundEnvelopes.Select(envelope => ((KafkaOffset)envelope.BrokerMessageIdentifier).TopicPartition)
            .ShouldBe(
                [
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic2", 1),
                    new TopicPartition("topic3", 1),
                    new TopicPartition("topic1", 0),
                    new TopicPartition("topic2", 1),
                    new TopicPartition("topic3", 1)
                ],
                ignoreOrder: true);
    }

    // The committed offsets are ignored: reconnecting, the messages are consumed again from the beginning
    private async Task ConsumeTwiceAsync(params string[] consumerNames)
    {
        for (int i = 0; i < 2; i++)
        {
            foreach (string consumerName in consumerNames)
            {
                await Helper.GetConsumer(consumerName).Client.ConnectAsync();
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            foreach (string consumerName in consumerNames)
            {
                await Helper.GetConsumer(consumerName).Client.DisconnectAsync();
            }
        }
    }

    private async Task ProduceToEachPartitionAsync(params string[] topics)
    {
        foreach (string topic in topics)
        {
            for (int partition = 0; partition < 3; partition++)
            {
                int targetPartition = partition;
                IProducer partitionProducer = Helper.GetProducer(producer => producer
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(topic, targetPartition)));

                await partitionProducer.ProduceAsync(new TestEventOne());
            }
        }
    }
}
