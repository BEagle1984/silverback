// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing;

public class DefaultProduceStrategyTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameType()
    {
        DefaultProduceStrategy strategy = new();
        DefaultProduceStrategy otherStrategy = new();

        strategy.Equals(otherStrategy).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentType()
    {
        DefaultProduceStrategy strategy = new();
        OutboxProduceStrategy otherStrategy = new(new InMemoryOutboxSettings());

        strategy.Equals(otherStrategy).ShouldBeFalse();
    }
}
