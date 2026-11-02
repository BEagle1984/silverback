// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Messaging.Broker.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class OffsetsTrackerTests
{
    [Fact]
    public void TrackOffset_ShouldAddOffsetFromTopicPartitionOffset()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new TopicPartitionOffset("topic", 1, 2));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 1, 3));

        tracker.GetRollbackOffSets().ShouldBe([new KafkaOffset("topic", 1, 2)]);
        tracker.GetCommitOffsets().ShouldBe([new KafkaOffset("topic", 1, 3)]);
    }

    [Fact]
    public void TrackOffset_ShouldKeepLatestCommitOffsetPerPartition()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new TopicPartitionOffset("topic", 1, 2));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 1, 3));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 2, 4));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 2, 5));

        tracker.GetCommitOffsets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 5)
            ],
            true);
    }

    [Fact]
    public void TrackOffset_ShouldKeepFirstRollbackOffsetPerPartition()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new TopicPartitionOffset("topic", 1, 2));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 1, 3));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 2, 4));
        tracker.TrackOffset(new TopicPartitionOffset("topic", 2, 5));

        tracker.GetRollbackOffSets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 2),
                new KafkaOffset("topic", 2, 4)
            ],
            true);
    }

    [Fact]
    public void UntrackPartition_ShouldRemovePartitionOffsets()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 3));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 4));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 5));

        tracker.UntrackPartition(new TopicPartition("topic", 1));

        tracker.GetRollbackOffSets().ShouldBe([new KafkaOffset("topic", 2, 4)]);
        tracker.GetCommitOffsets().ShouldBe([new KafkaOffset("topic", 2, 5)]);
    }

    [Fact]
    public void CommitOffset_ShouldUpdateRollbackOffset()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 3));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 4));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 5));

        tracker.Commit(new KafkaOffset("topic", 1, 2));

        tracker.GetRollbackOffSets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 4)
            ],
            true);
        tracker.GetCommitOffsets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 5)
            ],
            true);
    }

    [Fact]
    public void GetCommitOffsets_ShouldReturnLatestOffsetPerPartition()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 1));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 3));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 4));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 5));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 6));

        tracker.GetCommitOffsets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 6)
            ],
            true);
    }

    [Fact]
    public void GetRollbackOffsets_ShouldReturnFirstOffsetPerPartition()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 1));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 3));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 4));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 5));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 6));

        tracker.GetRollbackOffSets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 1),
                new KafkaOffset("topic", 2, 4)
            ],
            true);
    }

    [Fact]
    public void TrackPartition_ShouldTrackUnsetOffset_WhenPartitionIsNotTracked()
    {
        OffsetsTracker tracker = new();

        tracker.TrackPartition(new TopicPartition("topic", 1));

        tracker.GetRollbackOffSets().ShouldBe([new KafkaOffset("topic", 1, Offset.Unset)]);
        tracker.GetCommitOffsets().ShouldBe([new KafkaOffset("topic", 1, Offset.Unset)]);
    }

    [Fact]
    public void TrackPartition_ShouldKeepTrackedOffsets_WhenPartitionIsAlreadyTracked()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 3));

        tracker.TrackPartition(new TopicPartition("topic", 1));

        tracker.GetRollbackOffSets().ShouldBe([new KafkaOffset("topic", 1, 2)]);
        tracker.GetCommitOffsets().ShouldBe([new KafkaOffset("topic", 1, 3)]);
    }

    [Fact]
    public void GetReplayOffsets_ShouldReturnFirstUncommittedOffset()
    {
        OffsetsTracker tracker = new();

        for (int offset = 0; offset < 5; offset++)
        {
            tracker.TrackOffset(new KafkaOffset("topic", 1, offset));
        }

        tracker.Commit(new KafkaOffset("topic", 1, 0));
        tracker.Commit(new KafkaOffset("topic", 1, 1));

        tracker.GetReplayOffsets().ShouldBe([new KafkaOffset("topic", 1, 2)]);
    }

    [Fact]
    public void GetReplayOffsets_ShouldReturnNothing_WhenAllTrackedOffsetsAreCommitted()
    {
        OffsetsTracker tracker = new();

        for (int offset = 0; offset < 5; offset++)
        {
            tracker.TrackOffset(new KafkaOffset("topic", 1, offset));
            tracker.Commit(new KafkaOffset("topic", 1, offset));
        }

        tracker.GetReplayOffsets().ShouldBeEmpty();
    }

    [Fact]
    public void GetReplayOffsets_ShouldSkipPartition_WhenOnlySpecialOffsetIsTracked()
    {
        OffsetsTracker tracker = new();

        tracker.TrackPartition(new TopicPartition("topic", 1));
        tracker.TrackOffset(new KafkaOffset("topic", 2, Offset.End));

        tracker.GetReplayOffsets().ShouldBeEmpty();
    }

    [Fact]
    public void GetReplayOffsets_ShouldReturnOffsetPerPartition()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 1, 3));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 4));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 5));
        tracker.TrackOffset(new KafkaOffset("topic", 3, 6));
        tracker.TrackPartition(new TopicPartition("topic", 4));

        tracker.Commit(new KafkaOffset("topic", 2, 4));
        tracker.Commit(new KafkaOffset("topic", 3, 6));

        tracker.GetReplayOffsets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 2),
                new KafkaOffset("topic", 2, 5)
            ],
            true);
    }

    [Fact]
    public void Clear_ShouldRemoveAllOffsets()
    {
        OffsetsTracker tracker = new();

        tracker.TrackOffset(new KafkaOffset("topic", 1, 2));
        tracker.TrackOffset(new KafkaOffset("topic", 2, 4));
        tracker.TrackPartition(new TopicPartition("topic", 3));

        tracker.Clear();

        tracker.GetRollbackOffSets().ShouldBeEmpty();
        tracker.GetCommitOffsets().ShouldBeEmpty();
        tracker.GetReplayOffsets().ShouldBeEmpty();
    }

    [Fact]
    public void GetCommitIdentifiers_ShouldReturnLastOffsetPerPartition()
    {
        IBrokerMessageIdentifiersTracker tracker = new OffsetsTracker();

        tracker.TrackIdentifier(new KafkaOffset("topic", 1, 1));
        tracker.TrackIdentifier(new KafkaOffset("topic", 1, 2));
        tracker.TrackIdentifier(new KafkaOffset("topic", 1, 3));
        tracker.TrackIdentifier(new KafkaOffset("topic", 2, 4));
        tracker.TrackIdentifier(new KafkaOffset("topic", 2, 5));
        tracker.TrackIdentifier(new KafkaOffset("topic", 2, 6));

        tracker.GetCommitIdentifiers().ShouldBe(
            [
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 6)
            ],
            true);
    }

    [Fact]
    public void GetRollbackIdentifiers_ShouldReturnFirstOffsetPerPartition()
    {
        IBrokerMessageIdentifiersTracker tracker = new OffsetsTracker();

        tracker.TrackIdentifier(new KafkaOffset("topic", 1, 1));
        tracker.TrackIdentifier(new KafkaOffset("topic", 1, 2));
        tracker.TrackIdentifier(new KafkaOffset("topic", 1, 3));
        tracker.TrackIdentifier(new KafkaOffset("topic", 2, 4));
        tracker.TrackIdentifier(new KafkaOffset("topic", 2, 5));
        tracker.TrackIdentifier(new KafkaOffset("topic", 2, 6));

        tracker.GetRollbackIdentifiers().ShouldBe(
            [
                new KafkaOffset("topic", 1, 1),
                new KafkaOffset("topic", 2, 4)
            ],
            true);
    }
}
