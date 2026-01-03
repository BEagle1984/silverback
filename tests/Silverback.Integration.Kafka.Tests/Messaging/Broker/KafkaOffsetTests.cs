// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public class KafkaOffsetTests
{
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
