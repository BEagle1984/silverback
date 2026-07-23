// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public class MockedConfluentConsumerTests
{
    private static readonly TopicPartition Partition0 = new("topic", 0);

    private static readonly TopicPartition Partition1 = new("topic", 1);

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Not an issue")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Not an issue")]
    public async Task EnsurePartitionsAssigned_ShouldDiscardStaleAssignment_WhenRebalanceOverlapsAssignmentCallback()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        TaskCompletionSource assignmentCallbackCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim resumeAssignmentCompletion = new();
        List<IReadOnlyCollection<TopicPartition>> assignedCallbacks = [];
        int revokedCallbacks = 0;
        int connected = 0;

        consumer.PartitionsAssignedHandler = (_, partitions) =>
        {
            assignedCallbacks.Add([.. partitions]);
            Volatile.Write(ref connected, 1);

            if (assignedCallbacks.Count == 1)
                return GetPausedPartitionOffsets(partitions, assignmentCallbackCompleted, resumeAssignmentCompletion);

            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };
        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            Interlocked.Increment(ref revokedCallbacks);
            Volatile.Write(ref connected, 0);
            return partitions;
        };

        Task<bool> assignmentPass = Task.Run(() => consumer.EnsurePartitionsAssigned(CancellationToken.None));
        await assignmentCallbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            Volatile.Read(ref connected).ShouldBe(1);
            consumer.OnRebalancing();
            consumerGroup.Assignment = [Partition1];
            consumer.OnPartitionsRevoked([Partition0]);
        }
        finally
        {
            resumeAssignmentCompletion.Set();
        }

        (await assignmentPass.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeFalse();

        consumer.PartitionsAssigned.ShouldBeFalse();
        consumer.Assignment.ShouldBeEmpty();
        Volatile.Read(ref connected).ShouldBe(0);
        Volatile.Read(ref revokedCallbacks).ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(0);

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        assignedCallbacks.Count.ShouldBe(2);
        assignedCallbacks[1].ShouldBe([Partition1]);
        consumer.PartitionsAssigned.ShouldBeTrue();
        consumer.Assignment.ShouldBe(consumerGroup.Assignment);
        Volatile.Read(ref connected).ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(1);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Not an issue")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Not an issue")]
    public void EnsurePartitionsAssigned_ShouldDiscardStaleAssignment_WhenRebalanceIsTriggeredReentrantlyFromAssignmentCallback()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        List<IReadOnlyCollection<TopicPartition>> assignedCallbacks = [];
        int revokedCallbacks = 0;
        int connected = 0;

        consumer.PartitionsAssignedHandler = (_, partitions) =>
        {
            assignedCallbacks.Add([.. partitions]);

            if (assignedCallbacks.Count == 1)
            {
                consumer.OnRebalancing();
                consumerGroup.Assignment = [Partition1];
                consumer.OnPartitionsRevoked([Partition0]);
            }

            Volatile.Write(ref connected, 1);
            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };
        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            Interlocked.Increment(ref revokedCallbacks);
            Volatile.Write(ref connected, 0);
            return partitions;
        };

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeFalse();

        consumer.PartitionsAssigned.ShouldBeFalse();
        consumer.Assignment.ShouldBeEmpty();
        Volatile.Read(ref connected).ShouldBe(0);
        Volatile.Read(ref revokedCallbacks).ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(0);

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        assignedCallbacks.Count.ShouldBe(2);
        assignedCallbacks[1].ShouldBe([Partition1]);
        consumer.PartitionsAssigned.ShouldBeTrue();
        consumer.Assignment.ShouldBe(consumerGroup.Assignment);
        Volatile.Read(ref connected).ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(1);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Not an issue")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Not an issue")]
    public void EnsurePartitionsAssigned_ShouldRevalidateCallbackResult_WhenReentrantRebalanceDoesNotRevokePartitions()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        int assignedCallbacks = 0;
        int connected = 0;

        consumer.PartitionsAssignedHandler = (_, partitions) =>
        {
            if (Interlocked.Increment(ref assignedCallbacks) == 1)
                consumer.OnRebalancing();

            Volatile.Write(ref connected, 1);
            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeFalse();

        consumer.PartitionsAssigned.ShouldBeFalse();
        consumer.Assignment.ShouldBeEmpty();
        Volatile.Read(ref connected).ShouldBe(1);
        Volatile.Read(ref assignedCallbacks).ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(0);

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        consumer.PartitionsAssigned.ShouldBeTrue();
        consumer.Assignment.ShouldBe(consumerGroup.Assignment);
        Volatile.Read(ref connected).ShouldBe(1);
        Volatile.Read(ref assignedCallbacks).ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(1);
    }

    private static IEnumerable<TopicPartitionOffset> GetPausedPartitionOffsets(
        IEnumerable<TopicPartition> partitions,
        TaskCompletionSource assignmentCallbackCompleted,
        ManualResetEventSlim resumeAssignmentCompletion)
    {
        assignmentCallbackCompleted.TrySetResult();
        resumeAssignmentCompletion.Wait();

        foreach (TopicPartition partition in partitions)
        {
            yield return new TopicPartitionOffset(partition, Offset.Unset);
        }
    }

    private static MockedConfluentConsumer GetConsumer(FakeInternalMockedConsumerGroup consumerGroup)
    {
        MockedKafkaOptions options = new()
        {
            PartitionsAssignmentDelay = TimeSpan.Zero,
            OverriddenAutoCommitIntervalMs = null
        };

        return new MockedConfluentConsumer(
            new ConsumerConfig
            {
                BootstrapServers = "PLAINTEXT://mock",
                EnableAutoCommit = false,
                GroupId = "group"
            },
            new InMemoryTopicCollection(options),
            consumerGroup,
            options);
    }

    private sealed class FakeInternalMockedConsumerGroup : IInternalMockedConsumerGroup
    {
        public string GroupId => "group";

        public string BootstrapServers => "PLAINTEXT://mock";

        public IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets => [];

        public IReadOnlyCollection<TopicPartition> Assignment { get; set; } = [];

        public int AssignmentCompleteCount { get; private set; }

        public bool IsRebalancing { get; set; }

        public bool IsRebalanceScheduled { get; set; }

        public void Rebalance()
        {
        }

        public long GetCommittedOffsetsCount(string topic) => 0;

        public ValueTask WaitUntilAllMessagesAreConsumedAsync(
            IReadOnlyCollection<string> topicNames,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public void Subscribe(IMockedConfluentConsumer consumer, IEnumerable<string> topics)
        {
        }

        public void Unsubscribe(IMockedConfluentConsumer consumer)
        {
        }

        public void Assign(IMockedConfluentConsumer consumer, IEnumerable<TopicPartition> partitions) =>
            Assignment = [.. partitions];

        public void Unassign(IMockedConfluentConsumer consumer) => Assignment = [];

        public void Remove(IMockedConfluentConsumer consumer)
        {
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
        }

        public IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer) => Assignment;

        public TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition) => null;

        public void NotifyAssignmentComplete(MockedConfluentConsumer consumer) => AssignmentCompleteCount++;
    }
}
