// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlTableLockSettingsBuilderTests
{
    [Fact]
    public void Build_ShouldBuildSettings()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>();
        settings.ShouldBe(new PostgreSqlTableLockSettings("my-lock", "connection-string"));
    }

    [Fact]
    public void UseTable_ShouldSetLocksTableName()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.UseTable("test-locks").Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().TableName.ShouldBe("test-locks");
    }

    [Fact]
    public void WithDbCommandTimeout_ShouldSetDbCommandTimeout()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.WithDbCommandTimeout(TimeSpan.FromSeconds(42)).Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().DbCommandTimeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithDbCommandTimeout_ShouldThrow_WhenTimeoutIsLowerOrEqualToZero(int seconds)
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        Action act = () => builder.WithDbCommandTimeout(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("dbCommandTimeout");
    }

    [Fact]
    public void WithCreateTableTimeout_ShouldSetCreateTableTimeout()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.WithCreateTableTimeout(TimeSpan.FromSeconds(42)).Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().CreateTableTimeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithCreateTableTimeout_ShouldThrow_WhenTimeoutIsLowerOrEqualToZero(int seconds)
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        Action act = () => builder.WithCreateTableTimeout(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("createTableTimeout");
    }

    [Fact]
    public void WithAcquireInterval_ShouldSetAcquireInterval()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.WithAcquireInterval(TimeSpan.FromSeconds(42)).Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().AcquireInterval.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithAcquireInterval_ShouldThrow_WhenIntervalIsLowerOrEqualToZero(int seconds)
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        Action act = () => builder.WithAcquireInterval(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("acquireInterval");
    }

    [Fact]
    public void WithHeartbeatInterval_ShouldSetHeartbeatInterval()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.WithHeartbeatInterval(TimeSpan.FromSeconds(2)).Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().HeartbeatInterval.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithHeartbeatInterval_ShouldThrow_WhenIntervalIsLowerOrEqualToZero(int seconds)
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        Action act = () => builder.WithHeartbeatInterval(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("heartbeatInterval");
    }

    [Fact]
    public void WithLockTimeout_ShouldSetLockTimeout()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        DistributedLockSettings settings = builder.WithLockTimeout(TimeSpan.FromSeconds(42)).Build();

        settings.ShouldBeOfType<PostgreSqlTableLockSettings>().LockTimeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithLockTimeout_ShouldThrow_WhenTimeoutIsLowerOrEqualToZero(int seconds)
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        Action act = () => builder.WithLockTimeout(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("lockTimeout");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSettingsAreNotValid()
    {
        PostgreSqlTableLockSettingsBuilder builder = new("my-lock", "connection-string");

        Action act = () => builder.WithHeartbeatInterval(TimeSpan.FromSeconds(10)).WithLockTimeout(TimeSpan.FromSeconds(5)).Build();

        act.ShouldThrow<SilverbackConfigurationException>().Message.ShouldBe("LockTimeout must be greater than HeartbeatInterval.");
    }
}
