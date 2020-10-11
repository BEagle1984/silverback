// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.InMemory.Messaging.Broker
{
    public class InMemoryOffsetTests
    {
        [Fact(Skip = "Deprecated")]
        public void Constructor_WithKeyValueString_ProperlyConstructed()
        {
            var offset = new InMemoryOffset("key", "42");

            offset.Key.Should().Be("key");
            offset.Value.Should().Be("42");
            offset.Offset.Should().Be(42);
        }

        [Fact(Skip = "Deprecated")]
        public void Constructor_WithIntegerOffset_ProperlyConstructed()
        {
            var offset = new InMemoryOffset("key", 42);

            offset.Key.Should().Be("key");
            offset.Value.Should().Be("42");
            offset.Offset.Should().Be(42);
        }

        [Theory]
        [InlineData(5, 10, true)]
        [InlineData(5, 3, false)]
        [InlineData(5, 5, false)]
        public void LessThanOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
        {
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = new InMemoryOffset("key", valueB);

            var result = offsetA < offsetB;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(1, 3, false)]
        [InlineData(5, 5, false)]
        public void GreaterThanOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
        {
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = new InMemoryOffset("key", valueB);

            var result = offsetA > offsetB;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(5, 10, true)]
        [InlineData(5, 3, false)]
        [InlineData(5, 5, true)]
        public void LessThanOrEqualOperator_SomeOffsets_ProperlyCompared(int valueA, int valueB, bool expectedResult)
        {
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = new InMemoryOffset("key", valueB);

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
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = new InMemoryOffset("key", valueB);

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
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = valueB != null ? new InMemoryOffset("key", valueB.Value) : null;

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
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = valueB != null ? new InMemoryOffset("key", valueB.Value) : null;

            var result = offsetA != offsetB!;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(10, 5, 1)]
        [InlineData(1, 3, -1)]
        [InlineData(5, 5, 0)]
        [InlineData(5, null, 1)]
        public void CompareTo_AnotherInMemoryOffset_ProperlyCompared(int valueA, int? valueB, int expectedResult)
        {
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = valueB != null ? new InMemoryOffset("key", valueB.Value) : null;

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
            var offsetA = new InMemoryOffset("key", valueA);
            var offsetB = valueB != null ? new InMemoryOffset("key", valueB.Value) : null;

            var result = offsetA.CompareTo((IOffset?)offsetB);

            result.Should().Be(expectedResult);
        }

        [Fact(Skip = "Deprecated")]
        public void EqualsOffset_SameInstance_TrueReturned()
        {
            var offset = new InMemoryOffset("key", 42);

            var result = offset.Equals(offset);

            result.Should().BeTrue();
        }

        [Fact(Skip = "Deprecated")]
        public void EqualsObject_SameInstance_TrueReturned()
        {
            var offset = new InMemoryOffset("key", 42);

            var result = offset.Equals((object)offset);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("abc", 1, "abc", 1, true)]
        [InlineData("abc", 1, "abc", 2, false)]
        [InlineData("abc", 2, "abc", 1, false)]
        [InlineData("abc", 1, "def", 1, false)]
        public void EqualsOffset_AnotherInMemoryOffset_ProperlyCompared(
            string key1,
            int value1,
            string key2,
            int value2,
            bool expected)
        {
            var offset1 = new InMemoryOffset(key1, value1);
            var offset2 = new InMemoryOffset(key2, value2);

            var result = offset1.Equals(offset2);

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("abc", 1, "abc", 1, true)]
        [InlineData("abc", 1, "abc", 2, false)]
        [InlineData("abc", 2, "abc", 1, false)]
        [InlineData("abc", 1, "def", 1, false)]
        public void EqualsObject_AnotherInMemoryOffset_ProperlyCompared(
            string key1,
            int value1,
            string key2,
            int value2,
            bool expected)
        {
            var offset1 = new InMemoryOffset(key1, value1);
            var offset2 = new InMemoryOffset(key2, value2);

            var result = offset1.Equals((object)offset2);

            result.Should().Be(expected);
        }

        [Fact(Skip = "Deprecated")]
        public void EqualsOffset_Null_FalseReturned()
        {
            var offset1 = new InMemoryOffset("key", 42);

            var result = offset1.Equals(null);

            result.Should().BeFalse();
        }

        [Fact(Skip = "Deprecated")]
        public void EqualsObject_Null_FalseReturned()
        {
            var offset1 = new InMemoryOffset("key", 42);

            var result = offset1.Equals((object?)null);

            result.Should().BeFalse();
        }

        [Fact(Skip = "Deprecated")]
        public void EqualsOffset_DifferentOffsetType_FalseReturned()
        {
            var offset1 = new InMemoryOffset("key", 42);
            var offset2 = new TestOtherOffset("key", "42");

            var result = offset1.Equals(offset2);

            result.Should().BeFalse();
        }

        [Fact(Skip = "Deprecated")]
        public void EqualsObject_DifferentOffsetType_FalseReturned()
        {
            var offset1 = new InMemoryOffset("key", 42);
            var offset2 = new TestOtherOffset("key", "42");

            // ReSharper disable once SuspiciousTypeConversion.Global
            var result = offset1.Equals((object)offset2);

            result.Should().BeFalse();
        }

        private class TestOtherOffset : IOffset
        {
            public TestOtherOffset(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }

            public string Value { get; }

            public bool Equals(IOffset? other) => false;
        }
    }
}
