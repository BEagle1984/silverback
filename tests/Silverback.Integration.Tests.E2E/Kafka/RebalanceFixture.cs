// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
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

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        consumers[0].Client.Assignment.Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(5);
        consumers[1].Client.Assignment.Count.ShouldBe(0);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.ShouldNotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        consumers[0].Client.Assignment.Count.ShouldBe(3);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(3);
        consumers[1].Client.Assignment.Count.ShouldBe(2);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Count.ShouldBe(2);
    }

    [Fact]
    public async Task Rebalance_ShouldConsumeAgainAfterRebalance_WhenUsingCooperativeAssignmentStrategy()
    {
        PartitionCallbacksHandler partitionCallbacksHandler = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5))
                .ManuallyConnect())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithCooperativeStickyPartitionAssignmentStrategy()
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithCooperativeStickyPartitionAssignmentStrategy()
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

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        consumers[0].Client.Assignment.Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(5);
        consumers[1].Client.Assignment.Count.ShouldBe(0);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.ShouldNotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        consumers[0].Client.Assignment.Count.ShouldBe(3);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Count.ShouldBe(2);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(3);
        consumers[1].Client.Assignment.Count.ShouldBe(2);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Count.ShouldBe(2);
    }

    [Fact]
    public async Task Rebalance_ShouldCommitPendingOffsets()
    {
        int receivedMessages = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(10)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
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

        receivedMessages.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await DefaultConsumerGroup.RebalanceAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortPendingBatchesAndConsumeAgainAfterRebalance()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(10)))
                .AddConsumer(consumer => consumer
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

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(15);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        await DefaultConsumerGroup.RebalanceAsync();

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 20);

        receivedBatches.Count.ShouldBe(3);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        receivedBatches[2].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(20);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(3);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        receivedBatches[2].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    private sealed class PartitionCallbacksHandler : IKafkaPartitionsAssignedCallback, IKafkaPartitionsRevokedCallback
    {
        public ConcurrentDictionary<IKafkaConsumer, List<TopicPartition>> CurrentPartitions { get; } = new();

        public ConcurrentDictionary<IKafkaConsumer, List<TopicPartition>> RevokedPartitions { get; } = new();

        public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
            IReadOnlyCollection<TopicPartition> topicPartitions,
            IKafkaConsumer consumer)
        {
            List<TopicPartition> consumerPartitions = CurrentPartitions.GetOrAdd(
                consumer,
                _ => []);

            consumerPartitions.AddRange(topicPartitions);

            return null;
        }

        public void OnPartitionsRevoked(IReadOnlyCollection<TopicPartitionOffset> topicPartitionsOffset, IKafkaConsumer consumer)
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
