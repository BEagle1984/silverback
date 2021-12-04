// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxProduceStrategyTests
{
    [Fact]
    public void Equals_SameType_ReturnsTrue()
    {
        OutboxProduceStrategy strategy = new();
        OutboxProduceStrategy otherStrategy = new();

        strategy.Equals(otherStrategy).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        OutboxProduceStrategy strategy = new();
        DefaultProduceStrategy otherStrategy = new();

        strategy.Equals(otherStrategy).Should().BeFalse();
    }
}
