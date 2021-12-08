// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class RebalanceTests : KafkaTestFixture
{
    public RebalanceTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Rebalance_DefaultAssignmentStrategy_ConsumedAfterRebalance()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)),
                                2))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        DefaultConsumerGroup.Rebalance();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
    }

    [Fact]
    public async Task Rebalance_AddingConsumerWithDefaultAssignmentStrategy_ConsumedAfterRebalance()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber()
                    .AddSingletonBrokerCallbackHandler(partitionCallbacksHandler))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        Helper.Broker.AddConsumer(Helper.Broker.Consumers[0].Configuration);

        await AsyncTestingUtil.WaitAsync(() => Helper.Broker.Consumers.OfType<KafkaConsumer>().All(consumer => consumer.PartitionAssignment != null && consumer.PartitionAssignment.Count > 0));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        List<KafkaConsumer> kafkaConsumers = Helper.Broker.Consumers.OfType<KafkaConsumer>().ToList();
        kafkaConsumers.Should().HaveCount(2);

        foreach (KafkaConsumer consumer in kafkaConsumers)
        {
            consumer.IsConnected.Should().BeTrue();
            consumer.PartitionAssignment.Should().HaveCountGreaterOrEqualTo(1);
        }

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        partitionCallbacksHandler.CurrentPartitions[kafkaConsumers[0]].Should().HaveCount(3);
        partitionCallbacksHandler.CurrentPartitions[kafkaConsumers[1]].Should().HaveCount(2);
        partitionCallbacksHandler.RevokedPartitions[kafkaConsumers[0]].Should().HaveCount(5);
        partitionCallbacksHandler.RevokedPartitions.Should().NotContainKey(kafkaConsumers[1]);
    }

    [Fact]
    public async Task Rebalance_AddingConsumerWithCooperativeAssignmentStrategy_ConsumedAfterRebalance()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .WithPartitionAssignmentStrategy(PartitionAssignmentStrategy.CooperativeSticky))))
                    .AddIntegrationSpyAndSubscriber()
                    .AddSingletonBrokerCallbackHandler(partitionCallbacksHandler))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        Helper.Broker.AddConsumer(Helper.Broker.Consumers[0].Configuration);
        await AsyncTestingUtil.WaitAsync(() => Helper.Broker.Consumers.OfType<KafkaConsumer>().All(consumer => consumer.PartitionAssignment != null && consumer.PartitionAssignment.Count > 0));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        List<KafkaConsumer> kafkaConsumers = Helper.Broker.Consumers.OfType<KafkaConsumer>().ToList();
        kafkaConsumers.Should().HaveCount(2);

        foreach (KafkaConsumer consumer in kafkaConsumers)
        {
            consumer.IsConnected.Should().BeTrue();
            consumer.PartitionAssignment.Should().HaveCountGreaterOrEqualTo(1);
        }

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        partitionCallbacksHandler.CurrentPartitions[kafkaConsumers[0]].Should().HaveCount(3);
        partitionCallbacksHandler.CurrentPartitions[kafkaConsumers[1]].Should().HaveCount(2);
        partitionCallbacksHandler.RevokedPartitions[kafkaConsumers[0]].Should().HaveCount(2);
        partitionCallbacksHandler.RevokedPartitions.Should().NotContainKey(kafkaConsumers[1]);
    }

    [Fact]
    public async Task Rebalance_WithoutAutoCommit_PendingOffsetsCommitted()
    {
        int receivedMessages = 0;
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(10))))
                    .AddDelegateSubscriber((TestEventOne _) => Interlocked.Increment(ref receivedMessages)))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });

        await AsyncTestingUtil.WaitAsync(() => receivedMessages == 3);

        DefaultConsumerGroup.Rebalance();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task Rebalance_WithPendingBatch_AbortedAndConsumedAfterRebalance()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .DisableAutoCommit()
                                            .CommitOffsetEach(1))
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);
        receivedBatches.Sum(batch => batch.Count).Should().Be(15);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        DefaultConsumerGroup.Rebalance();

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 20);

        receivedBatches.Should().HaveCount(3);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        receivedBatches[2].Should().HaveCount(5);
        completedBatches.Should().Be(1);
        receivedBatches.Sum(batch => batch.Count).Should().Be(20);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(3);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        receivedBatches[2].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    private sealed class PartitionCallbacksHandler : IKafkaPartitionsAssignedCallback, IKafkaPartitionsRevokedCallback
    {
        public ConcurrentDictionary<KafkaConsumer, List<TopicPartition>> CurrentPartitions { get; } = new();

        public ConcurrentDictionary<KafkaConsumer, List<TopicPartition>> RevokedPartitions { get; } = new();

        public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
            IReadOnlyCollection<TopicPartition> topicPartitions,
            KafkaConsumer consumer)
        {
            List<TopicPartition> consumerPartitions = CurrentPartitions.GetOrAdd(
                consumer,
                _ => new List<TopicPartition>());

            consumerPartitions.AddRange(topicPartitions);

            return null;
        }

        public void OnPartitionsRevoked(IReadOnlyCollection<TopicPartitionOffset> topicPartitionsOffset, KafkaConsumer consumer)
        {
            CurrentPartitions.TryGetValue(consumer, out List<TopicPartition>? consumerPartitions);

            List<TopicPartition> revokedPartitions = RevokedPartitions.GetOrAdd(
                consumer,
                _ => new List<TopicPartition>());

            foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionsOffset)
            {
                revokedPartitions.Add(topicPartitionOffset.TopicPartition);
                consumerPartitions?.Remove(topicPartitionOffset.TopicPartition);
            }
        }
    }
}
