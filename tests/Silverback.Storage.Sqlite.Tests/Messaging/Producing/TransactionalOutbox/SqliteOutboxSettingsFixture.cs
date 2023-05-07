// Copyright (c) 2023 Sergio Aquilini
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
        settings.TableName.Should().Be("Silverback_Outbox");
    }

    [Fact]
    public void Constructor_ShouldSetConnectionStringAndTableName()
    {
        SqliteOutboxSettings settings = new("connection-string", "my-outbox");

        settings.ConnectionString.Should().Be("connection-string");
        settings.TableName.Should().Be("my-outbox");
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string", "my-outbox");

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<InMemoryLockSettings>();
        lockSettings.As<InMemoryLockSettings>().LockName.Should().Be($"outbox.{"connection-string".GetSha256Hash()}.my-outbox");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        SqliteOutboxSettings outboxSettings = new("connection-string", "my-outbox");

        Action act = outboxSettings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenTableNameIsEmptyOrWhitespace(string tableName)
    {
        SqliteOutboxSettings outboxSettings = new("connection-string", tableName);

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        SqliteOutboxSettings outboxSettings = new(connectionString!, "my-outbox");

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
