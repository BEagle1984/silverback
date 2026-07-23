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

public class MockedConsumerGroupTests
{
    private const string BootstrapServers = "PLAINTEXT://mock";

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

    private static async Task<RebalanceObservation> ObserveDisjointConsumerDuringJoinAsync(
        PartitionAssignmentStrategy assignmentStrategy)
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
