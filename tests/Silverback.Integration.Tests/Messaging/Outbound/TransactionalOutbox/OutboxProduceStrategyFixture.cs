// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxProduceStrategyFixture
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameTypeAndSettings()
    {
        OutboxProduceStrategy strategy1 = new(new TestSettings("outbox"));
        OutboxProduceStrategy strategy2 = new(new TestSettings("outbox"));

        strategy1.Equals(strategy2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentType()
    {
        OutboxProduceStrategy strategy1 = new(new TestSettings("outbox"));
        DefaultProduceStrategy strategy2 = new();

        strategy1.Equals(strategy2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingWithDifferentSettings()
    {
        OutboxProduceStrategy strategy1 = new(new TestSettings("outbox1"));
        OutboxProduceStrategy strategy2 = new(new TestSettings("outbox2"));

        strategy1.Equals(strategy2).Should().BeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing")]
    private record TestSettings(string Name) : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }
}
