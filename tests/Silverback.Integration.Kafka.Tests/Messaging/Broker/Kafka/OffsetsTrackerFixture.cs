// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Messaging.Broker.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class OffsetsTrackerFixture
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
            ignoreOrder: true);
        tracker.GetCommitOffsets().ShouldBe(
            [
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 5)
            ],
            ignoreOrder: true);
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
            ignoreOrder: true);
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
            ignoreOrder: true);
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
            ignoreOrder: true);
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
            ignoreOrder: true);
    }
}
