// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
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
            connected = 1;

            if (assignedCallbacks.Count == 1)
                return GetPausedPartitionOffsets(partitions, assignmentCallbackCompleted, resumeAssignmentCompletion);

            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };
        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            Interlocked.Increment(ref revokedCallbacks);
            connected = 0;
            return partitions;
        };

        Task<bool> assignmentPass = Task.Run(() => consumer.EnsurePartitionsAssigned(CancellationToken.None));
        await assignmentCallbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            connected.ShouldBe(1);
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
        connected.ShouldBe(0);
        revokedCallbacks.ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(0);

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        assignedCallbacks.Count.ShouldBe(2);
        assignedCallbacks[1].ShouldBe([Partition1]);
        consumer.PartitionsAssigned.ShouldBeTrue();
        consumer.Assignment.ShouldBe(consumerGroup.Assignment);
        connected.ShouldBe(1);
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

            connected = 1;
            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };
        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            Interlocked.Increment(ref revokedCallbacks);
            connected = 0;
            return partitions;
        };

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeFalse();

        consumer.PartitionsAssigned.ShouldBeFalse();
        consumer.Assignment.ShouldBeEmpty();
        connected.ShouldBe(0);
        revokedCallbacks.ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(0);

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        assignedCallbacks.Count.ShouldBe(2);
        assignedCallbacks[1].ShouldBe([Partition1]);
        consumer.PartitionsAssigned.ShouldBeTrue();
        consumer.Assignment.ShouldBe(consumerGroup.Assignment);
        connected.ShouldBe(1);
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

            connected = 1;
            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeFalse();

        consumer.PartitionsAssigned.ShouldBeFalse();
        consumer.Assignment.ShouldBeEmpty();
        connected.ShouldBe(1);
        assignedCallbacks.ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(0);

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        consumer.PartitionsAssigned.ShouldBeTrue();
        consumer.Assignment.ShouldBe(consumerGroup.Assignment);
        connected.ShouldBe(1);
        assignedCallbacks.ShouldBe(1);
        consumerGroup.AssignmentCompleteCount.ShouldBe(1);
    }

    [Fact]
    public void Unsubscribe_ShouldRevokeAssignment()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0, Partition1] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        List<TopicPartition> revokedPartitions = [];

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedPartitions.AddRange(partitions.Select(partition => partition.TopicPartition));
            return partitions;
        };

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        consumer.Unsubscribe();

        revokedPartitions.ShouldBe([Partition0, Partition1], ignoreOrder: true);
        consumer.Assignment.ShouldBeEmpty();
    }

    [Fact]
    public void Close_ShouldRevokeAssignment_WhenStillSubscribed()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0, Partition1] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        List<TopicPartition> revokedPartitions = [];

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedPartitions.AddRange(partitions.Select(partition => partition.TopicPartition));
            return partitions;
        };

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        consumer.Close();

        revokedPartitions.ShouldBe([Partition0, Partition1], ignoreOrder: true);
        consumer.Assignment.ShouldBeEmpty();
    }

    [Fact]
    public void Close_ShouldNotRevokeAssignmentAgain_WhenUnsubscribed()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        int revokedCallbacks = 0;

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedCallbacks++;
            return partitions;
        };

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();
        consumer.Unsubscribe();

        consumer.Close();

        revokedCallbacks.ShouldBe(1);
    }

    [Fact]
    public void Dispose_ShouldNotRevokeAssignment()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        int revokedCallbacks = 0;

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedCallbacks++;
            return partitions;
        };

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        consumer.Dispose();

        revokedCallbacks.ShouldBe(0);
        consumer.MemberId.ShouldBeEmpty();
    }

    [Fact]
    public void Close_ShouldNotRevokeManualAssignment()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new();
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        int revokedCallbacks = 0;

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedCallbacks++;
            return partitions;
        };

        consumer.Assign([new TopicPartitionOffset(Partition0, Offset.Unset)]);

        consumer.Close();

        revokedCallbacks.ShouldBe(0);
    }

    [Fact]
    public void MemberId_ShouldBeClearedAfterRevocation_WhenUnsubscribing()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        string? memberIdDuringRevocation = null;

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            memberIdDuringRevocation = consumer.MemberId;
            return partitions;
        };

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        consumer.Unsubscribe();

        memberIdDuringRevocation.ShouldNotBeNullOrEmpty();
        consumer.MemberId.ShouldBeEmpty();
    }

    [Fact]
    public void MemberId_ShouldBeKeptForConsumerLifetime_WhenUsingConsumerGroupProtocol()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup, groupProtocol: GroupProtocol.Consumer);
        string memberId = consumer.MemberId;

        memberId.ShouldNotBeEmpty();

        consumer.Subscribe("topic");
        consumer.Unsubscribe();
        consumer.Close();

        consumer.MemberId.ShouldBe(memberId);
    }

    [Fact]
    public void MemberId_ShouldBeSetOnlyWhileSubscribed()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);

        consumer.MemberId.ShouldBeEmpty();

        consumer.Subscribe("topic");
        string memberId = consumer.MemberId;

        memberId.ShouldNotBeEmpty();

        consumer.Subscribe("topic2");

        consumer.MemberId.ShouldBe(memberId);

        consumer.Unsubscribe();

        consumer.MemberId.ShouldBeEmpty();

        consumer.Subscribe("topic");

        consumer.MemberId.ShouldNotBeEmpty();
        consumer.MemberId.ShouldNotBe(memberId);

        consumer.Close();

        consumer.MemberId.ShouldBeEmpty();
    }

    [Fact]
    public void Unsubscribe_ShouldNotRevokeManualAssignment()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new();
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        int revokedCallbacks = 0;

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revokedCallbacks++;
            return partitions;
        };

        consumer.Assign([new TopicPartitionOffset(Partition0, Offset.Unset)]);

        consumer.Unsubscribe();

        revokedCallbacks.ShouldBe(0);
        consumer.Assignment.ShouldBe([Partition0]);
    }

    [Fact]
    public void Unsubscribe_ShouldReleaseAssignment_WhenRevokedHandlerFails()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);

        consumer.PartitionsRevokedHandler = (_, _) => throw new InvalidOperationException("test");

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        Action act = consumer.Unsubscribe;

        act.ShouldThrow<InvalidOperationException>();
        consumer.Assignment.ShouldBeEmpty();
        consumer.MemberId.ShouldBeEmpty();
    }

    [Fact]
    public void Subscribe_ShouldThrow_WhenPatternIsNotValid()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new();
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);

        Action act = () => consumer.Subscribe(["topic", "^topic-[a-z"]);

        KafkaException exception = act.ShouldThrow<KafkaException>();
        exception.Error.Code.ShouldBe(ErrorCode.Local_InvalidArg);
        consumer.Subscription.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Not an issue")]
    public async Task Unsubscribe_ShouldNotRevokeAgain_WhenRebalanceIsRevokingPartitions()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0, Partition1] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        TaskCompletionSource revoking = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseRevocation = new();
        ConcurrentQueue<TopicPartition> revokedPartitions = new();

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            revoking.TrySetResult();
            releaseRevocation.Wait(TimeSpan.FromSeconds(5));
            partitions.ForEach(partition => revokedPartitions.Enqueue(partition.TopicPartition));
            return partitions;
        };

        consumer.Subscribe("topic");
        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        // A rebalance revoking a partition on the group's thread
        Task rebalance = Task.Run(() => consumer.OnPartitionsRevoked([Partition0]));
        await revoking.Task.WaitAsync(TimeSpan.FromSeconds(5));

        consumer.Unsubscribe();
        releaseRevocation.Set();
        await rebalance.WaitAsync(TimeSpan.FromSeconds(5));

        revokedPartitions.ShouldBe([Partition0, Partition1]);
        consumer.Assignment.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Not an issue")]
    public void OnPartitionsRevoked_ShouldAutoCommit_WhenRevokedHandlerFails()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        using MockedConfluentConsumer consumer = GetConsumer(consumerGroup, true);

        consumer.PartitionsRevokedHandler = (_, _) => throw new InvalidOperationException("test");

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();
        consumer.StoreOffset(new TopicPartitionOffset(Partition0, 5));

        Action act = () => consumer.OnPartitionsRevoked([Partition0]);

        act.ShouldThrow<InvalidOperationException>();
        consumerGroup.Commits.ShouldBe([new TopicPartitionOffset(Partition0, 5)]);
        consumer.Assignment.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Not an issue")]
    public void OnPartitionsRevoked_ShouldProcessQueuedRevocation_WhenRevokedHandlerFails()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0, Partition1] };
        MockedConfluentConsumer consumer = GetConsumer(consumerGroup);
        int revokedCallbacks = 0;

        consumer.PartitionsRevokedHandler = (_, partitions) =>
        {
            if (++revokedCallbacks > 1)
                return partitions;

            // Queued until this callback returns
            consumer.OnPartitionsRevoked([Partition1]);
            throw new InvalidOperationException("test");
        };

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();

        Action act = () => consumer.OnPartitionsRevoked([Partition0]);

        act.ShouldThrow<InvalidOperationException>();
        revokedCallbacks.ShouldBe(2);
        consumer.Assignment.ShouldBeEmpty();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by Consume, before the disposal")]
    public void Consume_ShouldNotReturnPulledRecord_WhenPartitionIsSoughtMeanwhile()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        IInMemoryPartition partition = Substitute.For<IInMemoryPartition>();
        using MockedConfluentConsumer consumer = GetConsumer(consumerGroup, topics: GetTopics(partition));
        bool sought = false;
        partition.TryPull(Arg.Any<Offset>(), out Arg.Any<ConsumeResult<byte[]?, byte[]?>?>()).Returns(callInfo =>
        {
            callInfo[1] = new ConsumeResult<byte[]?, byte[]?>
            {
                Topic = Partition0.Topic,
                Partition = Partition0.Partition,
                Offset = callInfo.ArgAt<Offset>(0),
                Message = new Message<byte[]?, byte[]?>()
            };

            // Seek (e.g. a rollback) while the record is being pulled
            if (!sought)
            {
                sought = true;
                consumer.Seek(new TopicPartitionOffset(Partition0, 42));
            }

            return true;
        });

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();
        ConsumeResult<byte[]?, byte[]?>? result = consumer.Consume(TimeSpan.FromSeconds(5));

        result.ShouldNotBeNull();
        result.Offset.Value.ShouldBe(42);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task AutoCommit_ShouldInvokeOffsetsCommittedHandlerWithoutBlockingStoreOffset()
    {
        FakeInternalMockedConsumerGroup consumerGroup = new() { Assignment = [Partition0] };
        using MockedConfluentConsumer consumer = GetConsumer(consumerGroup, true, autoCommitIntervalMs: 10);
        TaskCompletionSource<bool> offsetStoredFromOtherThread = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.OffsetsCommittedHandler = (_, _) =>
        {
            if (offsetStoredFromOtherThread.Task.IsCompleted)
                return;

            bool stored = Task.Run(() => consumer.StoreOffset(new TopicPartitionOffset(Partition0, 6))).Wait(TimeSpan.FromSeconds(5));
            offsetStoredFromOtherThread.TrySetResult(stored);
        };

        consumer.EnsurePartitionsAssigned(CancellationToken.None).ShouldBeTrue();
        consumer.StoreOffset(new TopicPartitionOffset(Partition0, 5));

        (await offsetStoredFromOtherThread.Task.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBeTrue();
    }

    private static IInMemoryTopicCollection GetTopics(IInMemoryPartition partition)
    {
        IInMemoryTopic topic = Substitute.For<IInMemoryTopic>();
        topic.Partitions.Returns([partition]);

        IInMemoryTopicCollection topics = Substitute.For<IInMemoryTopicCollection>();
        topics.Get(Arg.Any<string>(), Arg.Any<ClientConfig>()).Returns(topic);

        return topics;
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

    private static MockedConfluentConsumer GetConsumer(
        FakeInternalMockedConsumerGroup consumerGroup,
        bool enableAutoCommit = false,
        GroupProtocol? groupProtocol = null,
        IInMemoryTopicCollection? topics = null,
        int? autoCommitIntervalMs = null)
    {
        MockedKafkaOptions options = new()
        {
            PartitionsAssignmentDelay = TimeSpan.Zero
        };

        return new MockedConfluentConsumer(
            new ConsumerConfig
            {
                BootstrapServers = "PLAINTEXT://mock",
                EnableAutoCommit = enableAutoCommit,
                AutoCommitIntervalMs = autoCommitIntervalMs,
                GroupId = "group",
                GroupProtocol = groupProtocol
            },
            topics ?? new InMemoryTopicCollection(options),
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

        public List<TopicPartitionOffset> Commits { get; } = [];

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

        public ValueTask WaitUntilAllMessagesAreCommittedAsync(
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

        public void Commit(IEnumerable<TopicPartitionOffset> offsets) => Commits.AddRange(offsets);

        public IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer) => Assignment;

        public TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition) => null;

        public void NotifyAssignmentComplete(MockedConfluentConsumer consumer) => AssignmentCompleteCount++;
    }
}
