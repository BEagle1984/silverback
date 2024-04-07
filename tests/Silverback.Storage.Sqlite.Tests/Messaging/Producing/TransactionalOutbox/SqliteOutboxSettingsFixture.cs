// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Storage.Sqlite.Messaging.Producing.TransactionalOutbox;

public class SqliteOutboxSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        SqliteOutboxSettings settings = new("connection-string");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("SilverbackOutbox");
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            TableName = "my-outbox"
        };

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<InMemoryLockSettings>();
        lockSettings.As<InMemoryLockSettings>().LockName.Should().Be($"outbox.{"connection-string".GetSha256Hash()}.my-outbox");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string");

        Action act = outboxSettings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsNullOrWhitespace(string? tableName)
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            TableName = tableName!
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        SqliteOutboxSettings outboxSettings = new(connectionString!);

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsLessThanZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsLessThanZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The create table timeout must be greater than zero.");
    }
}
