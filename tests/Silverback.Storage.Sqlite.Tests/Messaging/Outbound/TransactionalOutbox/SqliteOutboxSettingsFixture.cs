// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string", "my-outbox");

        Action act = () => outboxSettings.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsNullOrWhitespace(string? tableName)
    {
        SqliteOutboxSettings outboxSettings = new("connection-string") { TableName = tableName! };

        Action act = () => outboxSettings.Validate();

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        SqliteOutboxSettings outboxSettings = new(connectionString!, "my-outbox");

        Action act = () => outboxSettings.Validate();

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
