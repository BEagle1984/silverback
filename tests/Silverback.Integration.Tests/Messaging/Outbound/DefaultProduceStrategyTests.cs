// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound;

public class DefaultProduceStrategyTests
{
    [Fact]
    public void Equals_SameType_ReturnsTrue()
    {
        DefaultProduceStrategy strategy = new();
        DefaultProduceStrategy otherStrategy = new();

        strategy.Equals(otherStrategy).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        DefaultProduceStrategy strategy = new();
        OutboxProduceStrategy otherStrategy = new(new InMemoryOutboxSettings());

        strategy.Equals(otherStrategy).Should().BeFalse();
    }
}
