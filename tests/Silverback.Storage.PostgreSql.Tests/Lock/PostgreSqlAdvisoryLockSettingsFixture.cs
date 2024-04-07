// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlAdvisoryLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockNameAndConnectionString()
    {
        PostgreSqlAdvisoryLockSettings settings = new("my-lock", "connection-string");

        settings.LockName.Should().Be("my-lock");
        settings.ConnectionString.Should().Be("connection-string");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        PostgreSqlAdvisoryLockSettings settings = new("my-lock", "connection-string");

        Action act = settings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        PostgreSqlAdvisoryLockSettings settings = new(lockName!, "connection-string");

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The lock name is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        PostgreSqlAdvisoryLockSettings settings = new("my-lock", connectionString!);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The connection string is required.");
    }
}
