// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using FluentAssertions;
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

        tracker.GetRollbackOffSets().Should().BeEquivalentTo(new[] { new KafkaOffset("topic", 1, 2) });
        tracker.GetCommitOffsets().Should().BeEquivalentTo(new[] { new KafkaOffset("topic", 1, 3) });
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

        tracker.GetRollbackOffSets().Should().BeEquivalentTo(new[] { new KafkaOffset("topic", 2, 4) });
        tracker.GetCommitOffsets().Should().BeEquivalentTo(new[] { new KafkaOffset("topic", 2, 5) });
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

        tracker.GetRollbackOffSets().Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 4)
            });
        tracker.GetCommitOffsets().Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 5)
            });
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

        tracker.GetCommitOffsets().Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 6)
            });
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

        tracker.GetRollbackOffSets().Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic", 1, 1),
                new KafkaOffset("topic", 2, 4)
            });
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

        tracker.GetCommitIdentifiers().Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic", 1, 3),
                new KafkaOffset("topic", 2, 6)
            });
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

        tracker.GetRollbackIdentifiers().Should().BeEquivalentTo(
            new[]
            {
                new KafkaOffset("topic", 1, 1),
                new KafkaOffset("topic", 2, 4)
            });
    }
}
