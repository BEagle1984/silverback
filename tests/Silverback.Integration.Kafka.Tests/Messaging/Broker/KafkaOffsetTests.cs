// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public class KafkaOffsetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equality_ShouldIgnoreSourceChannel(bool bound)
    {
        KafkaOffset first = new(new TopicPartitionOffset("topic", 0, 42), Guid.NewGuid());
        KafkaOffset second = bound
            ? new KafkaOffset(new TopicPartitionOffset("topic", 0, 42), Guid.NewGuid())
            : new KafkaOffset("topic", 0, 42);

        first.Equals(second).ShouldBeTrue();
        first.Equals((IBrokerMessageIdentifier)second).ShouldBeTrue();
        first.Equals((object)second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        (first != second).ShouldBeFalse();
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.CompareTo(second).ShouldBe(0);

        HashSet<KafkaOffset> offsets = [first, second];

        offsets.ShouldHaveSingleItem();

        Dictionary<IBrokerMessageIdentifier, int> attempts = new() { [first] = 1 };

        attempts[second].ShouldBe(1);
    }

    [Fact]
    public void BelongsToChannel_ShouldMatchOnlySourceChannel()
    {
        Guid sourceChannelInstanceId = Guid.NewGuid();
        KafkaOffset offset = new(new TopicPartitionOffset("topic", 0, 42), sourceChannelInstanceId);

        offset.HasSourceChannel.ShouldBeTrue();
        offset.BelongsToChannel(sourceChannelInstanceId).ShouldBeTrue();
        offset.BelongsToChannel(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void BelongsToChannel_ShouldReturnFalse_WhenOffsetConstructedWithoutSourceChannel()
    {
        KafkaOffset offset = new("topic", 0, 42);

        offset.HasSourceChannel.ShouldBeFalse();
        offset.BelongsToChannel(Guid.NewGuid()).ShouldBeFalse();
        offset.BelongsToChannel(Guid.Empty).ShouldBeFalse();
    }

    [Fact]
    public void Copy_ShouldPreserveSourceChannel()
    {
        Guid sourceChannelInstanceId = Guid.NewGuid();
        KafkaOffset offset = new(new TopicPartitionOffset("topic", 0, 42), sourceChannelInstanceId);

        KafkaOffset copy = offset with { };

        copy.ShouldNotBeSameAs(offset);
        copy.ShouldBe(offset);
        copy.BelongsToChannel(sourceChannelInstanceId).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tracker_ShouldPreserveSourceChannel(bool bound)
    {
        Guid sourceChannelInstanceId = Guid.NewGuid();
        KafkaOffset offset = bound
            ? new KafkaOffset(new TopicPartitionOffset("topic", 0, 42), sourceChannelInstanceId)
            : new KafkaOffset("topic", 0, 42);

        OffsetsTracker tracker = new();
        tracker.TrackOffset(offset);

        tracker.GetCommitOffsets().ShouldHaveSingleItem().ShouldBeSameAs(offset);
        tracker.GetRollbackOffSets().ShouldHaveSingleItem().ShouldBeSameAs(offset);

        tracker.Commit(offset);

        KafkaOffset rollback = tracker.GetRollbackOffSets().ShouldHaveSingleItem();
        rollback.Offset.Value.ShouldBe(43);
        rollback.TopicPartition.ShouldBe(offset.TopicPartition);
        rollback.HasSourceChannel.ShouldBe(bound);
        rollback.BelongsToChannel(sourceChannelInstanceId).ShouldBe(bound);
        offset.Offset.Value.ShouldBe(42);
    }

    [Fact]
    public void Constructor_ShouldInitWithTopicPartitionOffset()
    {
        KafkaOffset offset = new(new TopicPartitionOffset("test-topic", 2, 42));

        offset.TopicPartition.Topic.ShouldBe("test-topic");
        offset.TopicPartition.Partition.Value.ShouldBe(2);
        offset.Offset.Value.ShouldBe(42);
    }

    [Theory]
    [InlineData(5, 10, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, false)]
    public void LessThanOperator_ShouldCompareOffsets(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA < offsetB;

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(1, 3, false)]
    [InlineData(5, 5, false)]
    public void GreaterThanOperator_ShouldCompareOffsets(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA > offsetB;

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(5, 10, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    public void LessThanOrEqualOperator_ShouldCompareOffsets(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA <= offsetB;

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(1, 3, false)]
    [InlineData(5, 5, true)]
    public void GreaterThanOrEqualOperator_ShouldCompareOffsets(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA >= offsetB;

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(5, 10, false)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    [InlineData(5, null, false)]
    public void EqualityOperator_ShouldCompareOffsets(int valueA, int? valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset? offsetB = valueB != null ? new KafkaOffset(new TopicPartitionOffset("test-topic", 2, valueB.Value)) : null;

        bool result = offsetA == offsetB!;

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(1, 3, true)]
    [InlineData(5, 5, false)]
    [InlineData(5, null, true)]
    public void InequalityOperator_ShouldCompareOffsets(int valueA, int? valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset? offsetB = valueB != null ? new KafkaOffset(new TopicPartitionOffset("test-topic", 2, valueB.Value)) : null;

        bool result = offsetA != offsetB!;

        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, 1)]
    [InlineData(1, 3, -1)]
    [InlineData(5, 5, 0)]
    [InlineData(5, null, 1)]
    public void CompareTo_ShouldCompareOffsets(int valueA, int? valueB, int expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset? offsetB = valueB != null ? new KafkaOffset(new TopicPartitionOffset("test-topic", 2, valueB.Value)) : null;

        int result = offsetA.CompareTo(offsetB);

        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameInstance()
    {
        KafkaOffset offset = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset.Equals(offset);

        result.ShouldBe(true);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameObjectInstance()
    {
        KafkaOffset offset = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset.Equals((object)offset);

        result.ShouldBe(true);
    }

    [Theory]
    [InlineData("abc", 0, 1, "abc", 0, 1, true)]
    [InlineData("abc", 0, 1, "abc", 1, 1, false)]
    [InlineData("abc", 0, 1, "abc", 0, 2, false)]
    [InlineData("abc", 0, 1, "def", 0, 1, false)]
    public void Equals_ShouldCompareWithOffset(
        string topic1,
        int partition1,
        long offset1,
        string topic2,
        int partition2,
        long offset2,
        bool expected)
    {
        KafkaOffset kafkaOffset1 = new(new TopicPartitionOffset(topic1, partition1, offset1));
        KafkaOffset kafkaOffset2 = new(new TopicPartitionOffset(topic2, partition2, offset2));

        bool result = kafkaOffset1.Equals(kafkaOffset2);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("abc", 0, 1, "abc", 0, 1, true)]
    [InlineData("abc", 0, 1, "abc", 1, 1, false)]
    [InlineData("abc", 0, 1, "abc", 0, 2, false)]
    [InlineData("abc", 0, 1, "def", 0, 1, false)]
    public void Equals_ShouldCompareWithObject(
        string topic1,
        int partition1,
        long offset1,
        string topic2,
        int partition2,
        long offset2,
        bool expected)
    {
        KafkaOffset kafkaOffset1 = new(new TopicPartitionOffset(topic1, partition1, offset1));
        KafkaOffset kafkaOffset2 = new(new TopicPartitionOffset(topic2, partition2, offset2));

        bool result = kafkaOffset1.Equals((object)kafkaOffset2);

        result.ShouldBe(expected);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenOtherOffsetIsNull()
    {
        KafkaOffset? offset1 = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset1.Equals(null);

        result.ShouldBe(false);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "Test code")]
    public void Equals_ShouldReturnFalse_WhenOtherObjectIsNull()
    {
        KafkaOffset? offset1 = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset1.Equals((object?)null);

        result.ShouldBe(false);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenOffsetTypeMismatch()
    {
        KafkaOffset offset1 = new(new TopicPartitionOffset("test-topic", 0, 42));
        TestOtherOffset offset2 = new("test-topic", "42");

        bool result = offset1.Equals(offset2);

        result.ShouldBe(false);
    }

    private sealed class TestOtherOffset : IBrokerMessageIdentifier
    {
        public TestOtherOffset(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }

        public string ToLogString() => Value;

        public string ToVerboseLogString() => Value;

        public bool Equals(IBrokerMessageIdentifier? other) => false;
    }
}
