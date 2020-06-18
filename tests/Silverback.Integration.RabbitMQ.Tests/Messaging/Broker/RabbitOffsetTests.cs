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
        public void ToLogString_Offset_StringReturned()
        {
            var offset = new RabbitOffset("test-queue", 42);

            var logString = offset.ToLogString();

            logString.Should().Be("42");
        }
    }
}
