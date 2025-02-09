// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlAdvisoryLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockNameAndConnectionString()
    {
        PostgreSqlAdvisoryLockSettings settings = new("my-lock", "connection-string");

        settings.LockName.ShouldBe("my-lock");
        settings.ConnectionString.ShouldBe("connection-string");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        PostgreSqlAdvisoryLockSettings settings = new("my-lock", "connection-string");

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        PostgreSqlAdvisoryLockSettings settings = new(lockName!, "connection-string");

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
        PostgreSqlAdvisoryLockSettings settings = new("my-lock", connectionString!);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The connection string is required.");
    }
}
