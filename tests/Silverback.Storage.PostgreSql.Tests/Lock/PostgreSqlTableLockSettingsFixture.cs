// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlTableLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockNameAndConnectionString()
    {
        PostgreSqlTableLockSettings settings = new("my-lock", "connection-string");

        settings.LockName.ShouldBe("my-lock");
        settings.ConnectionString.ShouldBe("connection-string");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        PostgreSqlTableLockSettings settings = new("my-lock", "connection-string");

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        PostgreSqlTableLockSettings settings = new(lockName!, "connection-string");

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The lock name is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        PostgreSqlTableLockSettings settings = new("my-lock", connectionString!);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The connection string is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbCommandTimeoutIsZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
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
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
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
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
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
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            CreateTableTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The create table timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAcquireIntervalIsZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            AcquireInterval = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The acquire interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAcquireIntervalIsLessThanZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            AcquireInterval = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The acquire interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenHeartbeatIntervalIsZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            HeartbeatInterval = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The heartbeat interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenHeartbeatIntervalIsLessThanZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            HeartbeatInterval = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The heartbeat interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLockTimeoutIsZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            LockTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The lock timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLockTimeoutIsLessThanZero()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            LockTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The lock timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLockTimeoutIsLessThanHeartbeatInterval()
    {
        PostgreSqlTableLockSettings outboxSettings = new("my-lock", "connection-string")
        {
            LockTimeout = TimeSpan.FromSeconds(1),
            HeartbeatInterval = TimeSpan.FromSeconds(2)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The lock timeout must be greater than the heartbeat interval.");
    }
}
