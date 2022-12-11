// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointFixture
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromStaticPartitionAssignment()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint.ConsumeFrom(
                                        new TopicPartition("topic1", 2),
                                        new TopicPartition("topic2", 2)))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producerTopic1Partition1 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 1)));
        IProducer producerTopic1Partition2 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 2)));
        IProducer producerTopic2Partition1 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 1)));
        IProducer producerTopic2Partition2 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 2)));

        await producerTopic1Partition1.ProduceAsync(new TestEventOne());
        await producerTopic1Partition2.ProduceAsync(new TestEventOne());
        await producerTopic2Partition1.ProduceAsync(new TestEventOne());
        await producerTopic2Partition2.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromStaticPartitionAssignment_WhenUsingResolver()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint.ConsumeFrom(
                                        DefaultTopicName,
                                        partitions => partitions.Skip(1)))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producerPartition0 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 0)));
        IProducer producerPartition1 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 1)));
        IProducer producerPartition2 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName, 2)));

        await producerPartition0.ProduceAsync(new TestEventOne());
        await producerPartition1.ProduceAsync(new TestEventOne());
        await producerPartition2.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromStaticPartitionAssignment_WhenUsingResolverForMultipleTopics()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint.ConsumeFrom(
                                        new[] { "topic1", "topic2" },
                                        _ => new TopicPartition[] { new("topic1", 2), new("topic2", 1) }))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producerTopic1Partition1 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 1)));
        IProducer producerTopic1Partition2 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1", 2)));
        IProducer producerTopic2Partition1 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 1)));
        IProducer producerTopic2Partition2 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2", 2)));

        await producerTopic1Partition1.ProduceAsync(new TestEventOne());
        await producerTopic1Partition2.ProduceAsync(new TestEventOne());
        await producerTopic2Partition1.ProduceAsync(new TestEventOne());
        await producerTopic2Partition2.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
    }
}
