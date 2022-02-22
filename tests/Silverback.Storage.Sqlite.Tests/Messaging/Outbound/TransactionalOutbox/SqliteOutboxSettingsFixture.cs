// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Outbound.TransactionalOutbox;

public class SqliteOutboxSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        SqliteOutboxSettings settings = new();

        settings.TableName.Should().Be("SilverbackOutbox");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameInstance()
    {
        SqliteOutboxSettings settings = new();

        settings.Equals(settings).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSettingsAreEquivalent()
    {
        SqliteOutboxSettings settings1 = new() { TableName = "outbox" };
        SqliteOutboxSettings settings2 = new() { TableName = "outbox" };

        settings1.Equals(settings2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenSettingsAreDifferent()
    {
        SqliteOutboxSettings settings1 = new() { TableName = "outbox1" };
        SqliteOutboxSettings settings2 = new() { TableName = "outbox2" };

        settings1.Equals(settings2).Should().BeFalse();
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string", "my-outbox");

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<InMemoryLockSettings>();
        lockSettings.As<InMemoryLockSettings>().LockName.Should().Be("outbox.connection-string.my-outbox");
    }
}
