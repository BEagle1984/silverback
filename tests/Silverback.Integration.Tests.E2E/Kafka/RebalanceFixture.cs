// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class RebalanceFixture : KafkaFixture
{
    public RebalanceFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Rebalance_ShouldConsumeAgainAfterRebalance_WhenUsingDefaultAssignmentStrategy()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                        .ManuallyConnect())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerClientCallback(partitionCallbacksHandler)
                .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().ToArray();
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        consumers[0].Client.Assignment.Should().HaveCount(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Should().HaveCount(5);
        consumers[1].Client.Assignment.Should().HaveCount(0);
        partitionCallbacksHandler.RevokedPartitions.Should().NotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.Should().NotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        consumers[0].Client.Assignment.Should().HaveCount(3);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Should().HaveCount(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Should().HaveCount(3);
        consumers[1].Client.Assignment.Should().HaveCount(2);
        partitionCallbacksHandler.RevokedPartitions.Should().NotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Should().HaveCount(2);
    }

    [Fact]
    public async Task Rebalance_ShouldConsumeAgainAfterRebalance_WhenUsingCooperativeAssignmentStrategy()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                        .ManuallyConnect())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .WithPartitionAssignmentStrategy(PartitionAssignmentStrategy.CooperativeSticky)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .WithPartitionAssignmentStrategy(PartitionAssignmentStrategy.CooperativeSticky)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerClientCallback(partitionCallbacksHandler)
                .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().ToArray();
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        consumers[0].Client.Assignment.Should().HaveCount(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Should().HaveCount(5);
        consumers[1].Client.Assignment.Should().HaveCount(0);
        partitionCallbacksHandler.RevokedPartitions.Should().NotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.Should().NotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        consumers[0].Client.Assignment.Should().HaveCount(3);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Should().HaveCount(2);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Should().HaveCount(3);
        consumers[1].Client.Assignment.Should().HaveCount(2);
        partitionCallbacksHandler.RevokedPartitions.Should().NotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Should().HaveCount(2);
    }

    [Fact]
    public async Task Rebalance_ShouldCommitPendingOffsets()
    {
        int receivedMessages = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(10)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(10)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        void HandleMessage(TestEventWithKafkaKey dummy) => Interlocked.Increment(ref receivedMessages);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages == 5);

        receivedMessages.Should().Be(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);

        await DefaultConsumerGroup.RebalanceAsync();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortPendingBatchesAndConsumeAgainAfterRebalance()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> eventsStream)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in eventsStream)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);
        receivedBatches.Sum(batch => batch.Count).Should().Be(15);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        await DefaultConsumerGroup.RebalanceAsync();

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 20, TimeSpan.FromSeconds(10));

        receivedBatches.Should().HaveCount(3);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        receivedBatches[2].Should().HaveCount(5);
        completedBatches.Should().Be(1);
        receivedBatches.Sum(batch => batch.Count).Should().Be(20);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
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
                _ => []);

            consumerPartitions.AddRange(topicPartitions);

            return null;
        }

        public void OnPartitionsRevoked(IReadOnlyCollection<TopicPartitionOffset> topicPartitionsOffset, KafkaConsumer consumer)
        {
            CurrentPartitions.TryGetValue(consumer, out List<TopicPartition>? consumerPartitions);

            List<TopicPartition> revokedPartitions = RevokedPartitions.GetOrAdd(
                consumer,
                _ => []);

            foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionsOffset)
            {
                revokedPartitions.Add(topicPartitionOffset.TopicPartition);
                consumerPartitions?.Remove(topicPartitionOffset.TopicPartition);
            }
        }
    }
}
