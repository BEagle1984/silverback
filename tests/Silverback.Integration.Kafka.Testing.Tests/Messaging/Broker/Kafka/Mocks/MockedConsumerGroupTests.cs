// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public partial class MockedConsumerGroupTests
{
    private const string BootstrapServers = "PLAINTEXT://mock";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldUseStoredOffsets_RegardlessOfCommit(bool commit)
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        using MockedConsumerGroup group = new("group", BootstrapServers, topics);
        using MockedConfluentConsumer consumer = GetConsumer(group, topics, options, PartitionAssignmentStrategy.RoundRobin);
        TopicPartition partition = new("topic", 0);
        topics.Get("topic", consumer.Config).Push(0, new Message<byte[]?, byte[]?> { Value = [1] }, Guid.Empty);
        consumer.Assign(new TopicPartitionOffset(partition, 0));
        consumer.Consume(TimeSpan.FromSeconds(1)).ShouldNotBeNull();

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        Task wait = group.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token).AsTask();
        Task committedWait = group.WaitUntilAllMessagesAreCommittedAsync([], timeout.Token).AsTask();

        wait.IsCompleted.ShouldBeFalse();

        consumer.StoreOffset(new TopicPartitionOffset(partition, 1));

        if (commit)
            consumer.Commit();

        await wait;
        await group.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token);

        consumer.GetStoredOffset(partition).Offset.ShouldBe(new Offset(1));
        group.CommittedOffsets.Count.ShouldBe(commit ? 1 : 0);

        if (!commit)
            committedWait.IsCompleted.ShouldBeFalse();

        consumer.Commit().Count.ShouldBe(commit ? 0 : 1);
        await committedWait;

        consumer.Seek(new TopicPartitionOffset(partition, 0));
        Task replayWait = group.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token).AsTask();

        replayWait.IsCompleted.ShouldBeFalse();

        consumer.StoreOffset(new TopicPartitionOffset(partition, 1));
        await replayWait;
    }

    [Fact]
    public async Task Rebalance_ShouldRevokeAndReassignUnchangedConsumer_WhenUsingRoundRobin()
    {
        RebalanceObservation observation =
            await ObserveDisjointConsumerDuringJoinAsync(PartitionAssignmentStrategy.RoundRobin);

        observation.AssignedCallbacks.Count.ShouldBe(2);
        observation.AssignedCallbacks[0].ShouldBe(observation.InitialAssignment, true);
        observation.AssignedCallbacks[1].ShouldBe(observation.InitialAssignment, true);
        observation.RevokedCallbacks.Count.ShouldBe(1);
        observation.RevokedCallbacks[0].ShouldBe(observation.InitialAssignment, true);
        observation.FinalAssignment.ShouldBe(observation.InitialAssignment, true);
    }

    [Fact]
    public async Task Rebalance_ShouldRetainPartitionsAndAssignEmptyIncrement_WhenUsingCooperativeSticky()
    {
        RebalanceObservation observation =
            await ObserveDisjointConsumerDuringJoinAsync(PartitionAssignmentStrategy.CooperativeSticky);

        observation.AssignedCallbacks.Count.ShouldBe(2);
        observation.AssignedCallbacks[0].ShouldBe(observation.InitialAssignment, true);
        observation.AssignedCallbacks[1].ShouldBeEmpty();
        observation.RevokedCallbacks.ShouldBeEmpty();
        observation.FinalAssignment.ShouldBe(observation.InitialAssignment, true);
    }

    [Fact]
    public async Task Remove_ShouldNotRebalance_WhenConsumerAlreadyUnsubscribed()
    {
        MockedKafkaOptions options = new()
        {
            PartitionsAssignmentDelay = TimeSpan.Zero
        };

        options.TopicPartitionsCount.Add("topic", 2);

        InMemoryTopicCollection topics = new(options);
        using MockedConsumerGroup consumerGroup = new("group", BootstrapServers, topics);
        MockedConfluentConsumer leavingConsumer = GetConsumer(consumerGroup, topics, options, PartitionAssignmentStrategy.RoundRobin);
        MockedConfluentConsumer remainingConsumer = GetConsumer(consumerGroup, topics, options, PartitionAssignmentStrategy.RoundRobin);
        int revokedCallbacks = 0;

        remainingConsumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            Interlocked.Increment(ref revokedCallbacks);
            return partitions;
        };

        leavingConsumer.Subscribe("topic");
        remainingConsumer.Subscribe("topic");

        using CancellationTokenSource consumerCancellationTokenSource = new();
        List<Task> consumerTasks =
        [
            StartConsumer(leavingConsumer, consumerCancellationTokenSource.Token),
            StartConsumer(remainingConsumer, consumerCancellationTokenSource.Token)
        ];

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            leavingConsumer.Unsubscribe();
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);
            int revokedCallbacksBeforeClose = revokedCallbacks;

            leavingConsumer.Close();
            consumerGroup.IsRebalanceScheduled.ShouldBeFalse();
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            revokedCallbacks.ShouldBe(revokedCallbacksBeforeClose);
            remainingConsumer.Assignment.Count.ShouldBe(2);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await Task.WhenAll(consumerTasks);
        }
    }

    [Fact]
    public async Task Subscribe_ShouldAssignPartitionsOfExistingTopicsMatchingPattern()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 2,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        topics.Get("topic-a", BootstrapServers);
        topics.Get("topic-b", BootstrapServers);
        topics.Get("other", BootstrapServers);
        topics.Get("topic-c", "PLAINTEXT://other-cluster");
        using MockedConsumerGroup consumerGroup = new("group", BootstrapServers, topics);
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup, topics, options, PartitionAssignmentStrategy.RoundRobin);

        consumer.Subscribe("^topic-");

        using CancellationTokenSource consumerCancellationTokenSource = new();
        Task consumerTask = StartConsumer(consumer, consumerCancellationTokenSource.Token);

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            consumer.Assignment.ShouldBe(
                [
                    new TopicPartition("topic-a", 0),
                    new TopicPartition("topic-a", 1),
                    new TopicPartition("topic-b", 0),
                    new TopicPartition("topic-b", 1)
                ],
                true);
            topics.Count.ShouldBe(4);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await consumerTask;
        }
    }

    [Fact]
    public async Task Subscribe_ShouldAssignPartitionsOfTopicsMatchingEntirePattern_WhenUsingConsumerProtocol()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        topics.Get("topic", BootstrapServers);
        topics.Get("topic-a", BootstrapServers);
        using MockedConsumerGroup consumerGroup = new("group", BootstrapServers, topics);
        MockedConfluentConsumer consumer = new(
            new ConsumerConfig
            {
                BootstrapServers = BootstrapServers,
                EnableAutoCommit = false,
                GroupId = "group",
                GroupProtocol = GroupProtocol.Consumer
            },
            topics,
            consumerGroup,
            options);

        consumer.Subscribe("^topic");

        using CancellationTokenSource consumerCancellationTokenSource = new();
        Task consumerTask = StartConsumer(consumer, consumerCancellationTokenSource.Token);

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            // Like the broker, the consumer protocol requires the pattern to match the entire topic name
            consumer.Assignment.ShouldBe([new TopicPartition("topic", 0)]);

            consumerGroup.OnTopicCreated(topics.Get("topic-b", BootstrapServers));

            consumerGroup.IsRebalanceScheduled.ShouldBeFalse();
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await consumerTask;
        }
    }

    [Fact]
    public async Task OnTopicCreated_ShouldAssignPartitionsOfNewTopic_WhenMatchingPattern()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        topics.Get("topic-a", BootstrapServers);
        using MockedConsumerGroup consumerGroup = new("group", BootstrapServers, topics);
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup, topics, options, PartitionAssignmentStrategy.RoundRobin);

        consumer.Subscribe(["other", "^topic-"]);

        using CancellationTokenSource consumerCancellationTokenSource = new();
        Task consumerTask = StartConsumer(consumer, consumerCancellationTokenSource.Token);

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            consumer.Assignment.ShouldBe([new TopicPartition("other", 0), new TopicPartition("topic-a", 0)], true);

            consumerGroup.OnTopicCreated(topics.Get("unrelated", BootstrapServers));
            consumerGroup.OnTopicCreated(topics.Get("topic-b", "PLAINTEXT://other-cluster"));

            consumerGroup.IsRebalanceScheduled.ShouldBeFalse();

            consumerGroup.OnTopicCreated(topics.Get("topic-b", BootstrapServers));
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            consumer.Assignment.ShouldBe(
                [
                    new TopicPartition("other", 0),
                    new TopicPartition("topic-a", 0),
                    new TopicPartition("topic-b", 0)
                ],
                true);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await consumerTask;
        }
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitForTopicsMatchingPattern()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        using MockedConsumerGroup group = new("group", BootstrapServers, topics);
        using MockedConfluentConsumer consumer = GetConsumer(group, topics, options, PartitionAssignmentStrategy.RoundRobin);
        TopicPartition patternPartition = new("topic-a", 0);
        TopicPartition otherPartition = new("other", 0);
        topics.Get("topic-a", consumer.Config).Push(0, new Message<byte[]?, byte[]?> { Value = [1] }, Guid.Empty);
        topics.Get("other", consumer.Config).Push(0, new Message<byte[]?, byte[]?> { Value = [2] }, Guid.Empty);
        consumer.Assign([new TopicPartitionOffset(patternPartition, 0), new TopicPartitionOffset(otherPartition, 0)]);
        consumer.Consume(TimeSpan.FromSeconds(1)).ShouldNotBeNull();
        consumer.Consume(TimeSpan.FromSeconds(1)).ShouldNotBeNull();

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        consumer.StoreOffset(new TopicPartitionOffset(otherPartition, 1));
        await group.WaitUntilAllMessagesAreConsumedAsync(["other"], timeout.Token);

        // The group is settled: the wait would complete synchronously if the pattern didn't match the pending partition
        Task patternWait = group.WaitUntilAllMessagesAreConsumedAsync(["^topic-"], timeout.Token).AsTask();

        patternWait.IsCompleted.ShouldBeFalse();

        consumer.StoreOffset(new TopicPartitionOffset(patternPartition, 1));
        await patternWait;
    }

    private static async Task<RebalanceObservation> ObserveDisjointConsumerDuringJoinAsync(PartitionAssignmentStrategy assignmentStrategy)
    {
        MockedKafkaOptions options = new()
        {
            PartitionsAssignmentDelay = TimeSpan.Zero
        };

        options.TopicPartitionsCount.Add("topic1", 4);
        options.TopicPartitionsCount.Add("topic2", 4);
        options.TopicPartitionsCount.Add("topic3", 4);
        options.TopicPartitionsCount.Add("topic4", 4);

        InMemoryTopicCollection topics = new(options);
        using MockedConsumerGroup consumerGroup = new("group", BootstrapServers, topics);
        MockedConfluentConsumer consumer1 = GetConsumer(consumerGroup, topics, options, assignmentStrategy);
        MockedConfluentConsumer unaffectedConsumer = GetConsumer(consumerGroup, topics, options, assignmentStrategy);
        ConcurrentQueue<TopicPartition[]> assignedCallbacks = new();
        ConcurrentQueue<TopicPartition[]> revokedCallbacks = new();

        unaffectedConsumer.PartitionsAssignedHandler = (_, partitions) =>
        {
            assignedCallbacks.Enqueue([.. partitions]);
            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };

        unaffectedConsumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedCallbacks.Enqueue([.. partitions.Select(partition => partition.TopicPartition)]);
            return partitions;
        };

        consumer1.Subscribe(["topic1", "topic2"]);
        unaffectedConsumer.Subscribe(["topic3", "topic4"]);

        using CancellationTokenSource consumerCancellationTokenSource = new();
        List<Task> consumerTasks =
        [
            StartConsumer(consumer1, consumerCancellationTokenSource.Token),
            StartConsumer(unaffectedConsumer, consumerCancellationTokenSource.Token)
        ];

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            TopicPartition[] initialAssignment = [.. unaffectedConsumer.Assignment];

            MockedConfluentConsumer joiningConsumer =
                GetConsumer(consumerGroup, topics, options, assignmentStrategy);

            joiningConsumer.Subscribe(["topic1", "topic2"]);
            consumerTasks.Add(StartConsumer(joiningConsumer, consumerCancellationTokenSource.Token));

            await WaitUntilAllPartitionsAreAssignedAsync(consumerGroup);

            return new RebalanceObservation(
                initialAssignment,
                [.. unaffectedConsumer.Assignment],
                [.. assignedCallbacks],
                [.. revokedCallbacks]);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await Task.WhenAll(consumerTasks);
        }
    }

    private static MockedConfluentConsumer GetConsumer(
        MockedConsumerGroup consumerGroup,
        IInMemoryTopicCollection topics,
        IMockedKafkaOptions options,
        PartitionAssignmentStrategy assignmentStrategy) =>
        new(
            new ConsumerConfig
            {
                BootstrapServers = BootstrapServers,
                EnableAutoCommit = false,
                GroupId = "group",
                PartitionAssignmentStrategy = assignmentStrategy
            },
            topics,
            consumerGroup,
            options);

    private static Task StartConsumer(MockedConfluentConsumer consumer, CancellationToken cancellationToken) =>
        Task.Run(
            () =>
            {
                try
                {
                    consumer.Consume(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Expected when stopping the background consume loop.
                }
            },
            CancellationToken.None);

    private static async Task WaitUntilAllPartitionsAreAssignedAsync(MockedConsumerGroup consumerGroup)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await consumerGroup.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token);
    }

    private sealed record RebalanceObservation(
        TopicPartition[] InitialAssignment,
        TopicPartition[] FinalAssignment,
        IReadOnlyList<TopicPartition[]> AssignedCallbacks,
        IReadOnlyList<TopicPartition[]> RevokedCallbacks);
}
