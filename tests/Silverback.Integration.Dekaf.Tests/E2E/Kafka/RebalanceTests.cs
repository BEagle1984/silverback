// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

// Adapted from Silverback.Integration.Tests.E2E: the tests relying on eager rebalancing are omitted, since Dekaf
// supports only the KIP-848 consumer protocol, where the rebalances are cooperative and the partitions that stay with
// their owner are never revoked:
// - Rebalance_ShouldConsumeAgainAfterRebalance_WhenUsingDefaultAssignmentStrategy
// - Rebalance_ShouldAbortPendingBatchesAndConsumeAgainAfterRebalance
// - Rebalance_ShouldAbortChunkSequenceAndNotCommit_WhenRebalancingWithIncompleteJson
// - Rebalance_ShouldAbortChunkSequenceAndNotCommit_WhenRebalancingWithIncompleteBinaryMessage
public class RebalanceTests : KafkaTests
{
    public RebalanceTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
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
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddSingletonBrokerClientCallback(partitionCallbacksHandler)
            .AddIntegrationSpyAndSubscriber());

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        consumers[0].Client.Assignment.Count.ShouldBe(5);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(5);
        consumers[1].Client.Assignment.Count.ShouldBe(0);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions.ShouldNotContainKey(consumers[1]);

        await consumers[1].Client.ConnectAsync();

        // Connecting schedules the rebalance; publish the next wave only after assignment completes
        await Helper.WaitUntilAllMessagesAreCommittedAsync();
        await AsyncTestingUtil.WaitAsync(() => consumers[0].Client.Assignment.Count + consumers[1].Client.Assignment.Count == 5 &&
                                               consumers[1].Client.Assignment.Count > 0);

        // The split depends on the assignor of the broker (2/3 or 3/2)
        int retainedPartitions = consumers[0].Client.Assignment.Count;
        retainedPartitions.ShouldBeInRange(2, 3);
        consumers[1].Client.Assignment.Count.ShouldBe(5 - retainedPartitions);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        consumers[0].Client.Assignment.Count.ShouldBe(retainedPartitions);
        partitionCallbacksHandler.RevokedPartitions[consumers[0]].Count.ShouldBe(5 - retainedPartitions);
        partitionCallbacksHandler.CurrentPartitions[consumers[0]].Count.ShouldBe(retainedPartitions);
        consumers[1].Client.Assignment.Count.ShouldBe(5 - retainedPartitions);
        partitionCallbacksHandler.RevokedPartitions.ShouldNotContainKey(consumers[1]);
        partitionCallbacksHandler.CurrentPartitions[consumers[1]].Count.ShouldBe(5 - retainedPartitions);
        consumers.ShouldAllBe(consumer => consumer.StatusInfo.Status >= ConsumerStatus.Connected);
    }

    [Fact]
    public async Task Rebalance_ShouldCommitPendingOffsets()
    {
        int receivedMessages = 0;

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
                    .CommitOffsetEach(10)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(10)
                    .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        void HandleMessage(TestEventWithKafkaKey dummy) => Interlocked.Increment(ref receivedMessages);

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = i });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages == 5);

        receivedMessages.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await consumers[1].Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) == 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Rebalance_ShouldAbortPendingBatchesAndConsumeAgainAfterRebalance_WhenUsingCooperativeAssignmentStrategy()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

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

        KafkaConsumer[] consumers = [.. Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>()];
        await consumers[0].Client.ConnectAsync();

        // Each producer writes to its own partition, to evenly distribute the messages
        IProducer[] producers =
        [
            .. Enumerable.Range(0, 5).Select(partition => Helper.GetProducer(producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(new TopicPartition(DefaultTopicName, partition)))))
        ];

        // Produce 1 1/2 batches per partition
        for (int i = 1; i <= 15; i++)
        {
            await producers[0].ProduceAsync(new TestEventOne());
            await producers[1].ProduceAsync(new TestEventOne());
            await producers[2].ProduceAsync(new TestEventOne());
            await producers[3].ProduceAsync(new TestEventOne());
            await producers[4].ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15 * 5);

        receivedBatches.Count.ShouldBe(10);
        completedBatches.ShouldBe(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(15 * 5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10 * 5);

        // Connect the second consumer to trigger the rebalance
        await consumers[1].Client.ConnectAsync();

        // Cooperative assignment: only the partitions moved to the new consumer are revoked (2 or 3, depending on the assignor)
        await AsyncTestingUtil.WaitAsync(() => consumers[1].Client.Assignment.Count > 0);
        int movedPartitions = consumers[1].Client.Assignment.Count;
        movedPartitions.ShouldBeInRange(2, 3);

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == (15 * 5) + (5 * movedPartitions));

        receivedBatches.Count.ShouldBe(10 + movedPartitions);
        completedBatches.ShouldBe(5);
        receivedBatches.Sum(batch => batch.Count).ShouldBe((15 * 5) + (5 * movedPartitions));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10 * 5);

        // Produce another 1/2 batche per reassigned partition
        for (int i = 1; i <= 5; i++)
        {
            await producers[0].ProduceAsync(new TestEventOne());
            await producers[1].ProduceAsync(new TestEventOne());
            await producers[2].ProduceAsync(new TestEventOne());
            await producers[3].ProduceAsync(new TestEventOne());
            await producers[4].ProduceAsync(new TestEventOne());
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(10 + movedPartitions);
        completedBatches.ShouldBe(10);
        receivedBatches.Sum(batch => batch.Count).ShouldBe((20 * 5) + (5 * movedPartitions));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20 * 5);
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
