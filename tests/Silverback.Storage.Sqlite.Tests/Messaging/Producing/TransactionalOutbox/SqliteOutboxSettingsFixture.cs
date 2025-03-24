// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
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

        settings.ConnectionString.ShouldBe("connection-string");
        settings.TableName.ShouldBe("SilverbackOutbox");
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            TableName = "my-outbox"
        };

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.ShouldBeOfType<InMemoryLockSettings>().LockName.ShouldBe($"outbox.{"connection-string".GetSha256Hash()}.my-outbox");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string");

        Action act = outboxSettings.Validate;

        act.ShouldNotThrow();
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

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        SqliteOutboxSettings outboxSettings = new(connectionString!);

        Action act = outboxSettings.Validate;

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsLessThanZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            DbCommandTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The command timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The create table timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenCreateTableTimeoutIsLessThanZero()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string")
        {
            CreateTableTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The create table timeout must be greater than zero.");
    }
}
