// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging.Broker
{
    public class RabbitOffsetTests
    {
        [Fact]
        public void Constructor_WithKeyValueString_ProperlyConstructed()
        {
            var offset = new RabbitOffset("test-queue", "42");

            offset.Key.Should().Be("test-queue");
            offset.Value.Should().Be("42");

            offset.ConsumerTag.Should().Be("test-queue");
            offset.DeliveryTag.Should().Be(42);
        }

        [Fact]
        public void Constructor_WithTopicPartitionOffset_ProperlyConstructed()
        {
            var offset = new RabbitOffset("test-queue", 42);

            offset.Key.Should().Be("test-queue");
            offset.Value.Should().Be("42");

            offset.ConsumerTag.Should().Be("test-queue");
            offset.DeliveryTag.Should().Be(42);
        }

        [Fact]
        public void EqualsOffset_SameInstance_TrueReturned()
        {
            var offset = new RabbitOffset("test-queue", 42);

            var result = offset.Equals(offset);

            result.Should().BeTrue();
        }

        [Fact]
        public void EqualsObject_SameInstance_TrueReturned()
        {
            var offset = new RabbitOffset("test-queue", 42);

            var result = offset.Equals((object)offset);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("abc", 1, "abc", 1, true)]
        [InlineData("abc", 1, "abc", 2, false)]
        [InlineData("abc", 2, "abc", 1, false)]
        [InlineData("abc", 1, "def", 1, false)]
        public void EqualsOffset_AnotherRabbitOffset_ProperlyCompared(
            string consumerTag1,
            ulong deliveryTag1,
            string consumerTag2,
            ulong deliveryTag2,
            bool expected)
        {
            var offset1 = new RabbitOffset(consumerTag1, deliveryTag1);
            var offset2 = new RabbitOffset(consumerTag2, deliveryTag2);

            var result = offset1.Equals(offset2);

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("abc", 1, "abc", 1, true)]
        [InlineData("abc", 1, "abc", 2, false)]
        [InlineData("abc", 2, "abc", 1, false)]
        [InlineData("abc", 1, "def", 1, false)]
        public void EqualsObject_AnotherRabbitOffset_ProperlyCompared(
            string consumerTag1,
            ulong deliveryTag1,
            string consumerTag2,
            ulong deliveryTag2,
            bool expected)
        {
            var offset1 = new RabbitOffset(consumerTag1, deliveryTag1);
            var offset2 = new RabbitOffset(consumerTag2, deliveryTag2);

            var result = offset1.Equals((object)offset2);

            result.Should().Be(expected);
        }

        [Fact]
        public void EqualsOffset_Null_FalseReturned()
        {
            var offset1 = new RabbitOffset("test-queue", 42);

            var result = offset1.Equals(null);

            result.Should().BeFalse();
        }

        [Fact]
        public void EqualsObject_Null_FalseReturned()
        {
            var offset1 = new RabbitOffset("test-queue", 42);

            var result = offset1.Equals((object?)null);

            result.Should().BeFalse();
        }

        [Fact]
        public void EqualsOffset_DifferentOffsetType_FalseReturned()
        {
            var offset1 = new RabbitOffset("test-queue", 42);
            var offset2 = new TestOtherOffset("test-queue", "42");

            var result = offset1.Equals(offset2);

            result.Should().BeFalse();
        }

        [Fact]
        public void EqualsObject_DifferentOffsetType_FalseReturned()
        {
            var offset1 = new RabbitOffset("test-queue", 42);
            var offset2 = new TestOtherOffset("test-queue", "42");

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
