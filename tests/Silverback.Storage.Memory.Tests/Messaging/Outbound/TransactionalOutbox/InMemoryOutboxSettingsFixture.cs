// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Outbound.TransactionalOutbox;

public class InMemoryOutboxSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        InMemoryOutboxSettings settings = new();

        settings.OutboxName.Should().Be("default");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameInstance()
    {
        InMemoryOutboxSettings settings = new();

        settings.Equals(settings).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSettingsAreEquivalent()
    {
        InMemoryOutboxSettings settings1 = new() { OutboxName = "outbox" };
        InMemoryOutboxSettings settings2 = new() { OutboxName = "outbox" };

        settings1.Equals(settings2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenSettingsAreDifferent()
    {
        InMemoryOutboxSettings settings1 = new() { OutboxName = "outbox1" };
        InMemoryOutboxSettings settings2 = new() { OutboxName = "outbox2" };

        settings1.Equals(settings2).Should().BeFalse();
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        InMemoryOutboxSettings outboxSettings = new("my-outbox");

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<InMemoryLockSettings>();
        lockSettings.As<InMemoryLockSettings>().LockName.Should().Be("outbox.my-outbox");
    }
}
