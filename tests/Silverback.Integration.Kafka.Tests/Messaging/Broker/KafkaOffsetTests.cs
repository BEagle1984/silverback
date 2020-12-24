// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker
{
    public class KafkaOffsetTests
    {
        [Fact]
        public void Constructor_WithKeyValueString_ProperlyConstructed()
        {
            var offset = new KafkaOffset("test-topic[2]", "42");

            offset.Key.Should().Be("test-topic[2]");
            offset.Value.Should().Be("42");

            offset.Topic.Should().Be("test-topic");
            offset.Partition.Should().Be(2);
            offset.Offset.Should().Be(42);
        }

        [Fact]
        public void Constructor_WithTopicPartitionOffset_ProperlyConstructed()
        {
            var offset = new KafkaOffset("test-topic", 2, 42);

            offset.Key.Should().Be("test-topic[2]");
            offset.Value.Should().Be("42");

            offset.Topic.Should().Be("test-topic");
            offset.Partition.Should().Be(2);
            offset.Offset.Should().Be(42);
        }

        [Theory]
        [InlineData(5, 10, true)]
        [InlineData(5, 3, false)]
        [InlineData(5, 5, false)]
        public void LessThanOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = new KafkaOffset("test-topic", 2, valueB);

            var result = offsetA < offsetB;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(1, 3, false)]
        [InlineData(5, 5, false)]
        public void GreaterThanOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = new KafkaOffset("test-topic", 2, valueB);

            var result = offsetA > offsetB;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(5, 10, true)]
        [InlineData(5, 3, false)]
        [InlineData(5, 5, true)]
        public void LessThanOrEqualOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = new KafkaOffset("test-topic", 2, valueB);

            var result = offsetA <= offsetB;

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
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = new KafkaOffset("test-topic", 2, valueB);

            var result = offsetA >= offsetB;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(5, 10, false)]
        [InlineData(5, 3, false)]
        [InlineData(5, 5, true)]
        [InlineData(5, null, false)]
        public void EqualityOperator_SomeOffsets_ProperlyCompared(int valueA, int? valueB, bool expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = valueB != null ? new KafkaOffset("test-topic", 2, valueB.Value) : null;

            var result = offsetA == offsetB!;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(1, 3, true)]
        [InlineData(5, 5, false)]
        [InlineData(5, null, true)]
        public void InequalityOperator_SomeOffsets_ProperlyCompared(int valueA, int? valueB, bool expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = valueB != null ? new KafkaOffset("test-topic", 2, valueB.Value) : null;

            var result = offsetA != offsetB!;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(10, 5, 1)]
        [InlineData(1, 3, -1)]
        [InlineData(5, 5, 0)]
        [InlineData(5, null, 1)]
        public void CompareTo_AnotherKafkaOffset_ProperlyCompared(int valueA, int? valueB, int expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = valueB != null ? new KafkaOffset("test-topic", 2, valueB.Value) : null;

            var result = offsetA.CompareTo(offsetB);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(10, 5, 1)]
        [InlineData(1, 3, -1)]
        [InlineData(5, 5, 0)]
        [InlineData(5, null, 1)]
        public void CompareTo_AnotherOffset_ProperlyCompared(int valueA, int? valueB, int expectedResult)
        {
            var offsetA = new KafkaOffset("test-topic", 2, valueA);
            var offsetB = valueB != null ? new KafkaOffset("test-topic", 2, valueB.Value) : null;

            var result = offsetA.CompareTo((IBrokerMessageOffset?)offsetB);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void EqualsOffset_SameInstance_TrueReturned()
        {
            var offset = new KafkaOffset("test-topic", 0, 42);

            var result = offset.Equals(offset);

            result.Should().BeTrue();
        }

        [Fact]
        public void EqualsObject_SameInstance_TrueReturned()
        {
            var offset = new KafkaOffset("test-topic", 0, 42);

            var result = offset.Equals((object)offset);

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
            var kafkaOffset1 = new KafkaOffset(topic1, partition1, offset1);
            var kafkaOffset2 = new KafkaOffset(topic2, partition2, offset2);

            var result = kafkaOffset1.Equals(kafkaOffset2);

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
            var kafkaOffset1 = new KafkaOffset(topic1, partition1, offset1);
            var kafkaOffset2 = new KafkaOffset(topic2, partition2, offset2);

            var result = kafkaOffset1.Equals((object)kafkaOffset2);

            result.Should().Be(expected);
        }

        [Fact]
        [SuppressMessage("", "CA1508", Justification = "Test code")]
        public void EqualsOffset_Null_FalseReturned()
        {
            var offset1 = new KafkaOffset("test-topic", 0, 42);

            var result = offset1.Equals(null);

            result.Should().BeFalse();
        }

        [Fact]
        [SuppressMessage("", "CA1508", Justification = "Test code")]
        public void EqualsObject_Null_FalseReturned()
        {
            var offset1 = new KafkaOffset("test-topic", 0, 42);

            var result = offset1.Equals((object?)null);

            result.Should().BeFalse();
        }

        [Fact]
        public void EqualsOffset_DifferentOffsetType_FalseReturned()
        {
            var offset1 = new KafkaOffset("test-topic", 0, 42);
            var offset2 = new TestOtherOffset("test-topic", "42");

            var result = offset1.Equals(offset2);

            result.Should().BeFalse();
        }

        [Fact]
        public void EqualsObject_DifferentOffsetType_FalseReturned()
        {
            var offset1 = new KafkaOffset("test-topic", 0, 42);
            var offset2 = new TestOtherOffset("test-queue", "42");

            // ReSharper disable once SuspiciousTypeConversion.Global
            var result = offset1.Equals((object)offset2);

            result.Should().BeFalse();
        }

        private class TestOtherOffset : IBrokerMessageOffset
        {
            public TestOtherOffset(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }

            public string Value { get; }

            public bool Equals(IBrokerMessageIdentifier? other) => false;

            public int CompareTo(IBrokerMessageOffset? other) =>
                string.Compare(Value, other?.Value, StringComparison.Ordinal);
        }
    }
}
