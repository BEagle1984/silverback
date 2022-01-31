// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public class KafkaOffsetTests
{
    [Fact]
    public void Constructor_WithTopicPartitionOffset_ProperlyConstructed()
    {
        KafkaOffset offset = new(new TopicPartitionOffset("test-topic", 2, 42));

        offset.TopicPartition.Topic.Should().Be("test-topic");
        offset.TopicPartition.Partition.Value.Should().Be(2);
        offset.Offset.Value.Should().Be(42);
    }

    [Theory]
    [InlineData(5, 10, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, false)]
    public void LessThanOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA < offsetB;

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(1, 3, false)]
    [InlineData(5, 5, false)]
    public void GreaterThanOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA > offsetB;

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(5, 10, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    public void LessThanOrEqualOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA <= offsetB;

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(1, 3, false)]
    [InlineData(5, 5, true)]
    public void GreaterThanOrEqualOperator_SomeOffsets_ProperlyCompared(
        int valueA,
        int valueB,
        bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset offsetB = new(new TopicPartitionOffset("test-topic", 2, valueB));

        bool result = offsetA >= offsetB;

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(5, 10, false)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    [InlineData(5, null, false)]
    public void EqualityOperator_SomeOffsets_ProperlyCompared(int valueA, int? valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset? offsetB = valueB != null ? new KafkaOffset(new TopicPartitionOffset("test-topic", 2, valueB.Value)) : null;

        bool result = offsetA == offsetB!;

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(1, 3, true)]
    [InlineData(5, 5, false)]
    [InlineData(5, null, true)]
    public void InequalityOperator_SomeOffsets_ProperlyCompared(int valueA, int? valueB, bool expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset? offsetB = valueB != null ? new KafkaOffset(new TopicPartitionOffset("test-topic", 2, valueB.Value)) : null;

        bool result = offsetA != offsetB!;

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(10, 5, 1)]
    [InlineData(1, 3, -1)]
    [InlineData(5, 5, 0)]
    [InlineData(5, null, 1)]
    public void CompareTo_AnotherKafkaOffset_ProperlyCompared(int valueA, int? valueB, int expectedResult)
    {
        KafkaOffset offsetA = new(new TopicPartitionOffset("test-topic", 2, valueA));
        KafkaOffset? offsetB = valueB != null ? new KafkaOffset(new TopicPartitionOffset("test-topic", 2, valueB.Value)) : null;

        int result = offsetA.CompareTo(offsetB);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void EqualsOffset_SameInstance_TrueReturned()
    {
        KafkaOffset offset = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset.Equals(offset);

        result.Should().BeTrue();
    }

    [Fact]
    public void EqualsObject_SameInstance_TrueReturned()
    {
        KafkaOffset offset = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset.Equals((object)offset);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("abc", 0, 1, "abc", 0, 1, true)]
    [InlineData("abc", 0, 1, "abc", 1, 1, false)]
    [InlineData("abc", 0, 1, "abc", 0, 2, false)]
    [InlineData("abc", 0, 1, "def", 0, 1, false)]
    public void EqualsOffset_AnotherKafkaOffset_ProperlyCompared(
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

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", 0, 1, "abc", 0, 1, true)]
    [InlineData("abc", 0, 1, "abc", 1, 1, false)]
    [InlineData("abc", 0, 1, "abc", 0, 2, false)]
    [InlineData("abc", 0, 1, "def", 0, 1, false)]
    public void EqualsObject_AnotherKafkaOffset_ProperlyCompared(
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

        result.Should().Be(expected);
    }

    [Fact]
    [SuppressMessage("", "CA1508", Justification = "Test code")]
    public void EqualsOffset_Null_FalseReturned()
    {
        KafkaOffset? offset1 = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset1.Equals(null);

        result.Should().BeFalse();
    }

    [Fact]
    [SuppressMessage("", "CA1508", Justification = "Test code")]
    public void EqualsObject_Null_FalseReturned()
    {
        KafkaOffset? offset1 = new(new TopicPartitionOffset("test-topic", 0, 42));

        bool result = offset1.Equals((object?)null);

        result.Should().BeFalse();
    }

    [Fact]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "Test code")]
    public void Equals_DifferentOffsetType_FalseReturned()
    {
        KafkaOffset offset1 = new(new TopicPartitionOffset("test-topic", 0, 42));
        TestOtherOffset offset2 = new("test-topic", "42");

        bool result = offset1.Equals(offset2);

        result.Should().BeFalse();
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
