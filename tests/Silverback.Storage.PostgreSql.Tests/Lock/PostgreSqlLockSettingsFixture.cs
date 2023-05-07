// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlLockSettingsFixture
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameInstance()
    {
        PostgreSqlLockSettings settings = new("lock", "connection");

        settings.Equals(settings).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSettingsAreEquivalent()
    {
        PostgreSqlLockSettings settings1 = new("lock", "connection");
        PostgreSqlLockSettings settings2 = new("lock", "connection");

        settings1.Equals(settings2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenLockNameIsDifferent()
    {
        PostgreSqlLockSettings settings1 = new("lock1", "connection");
        PostgreSqlLockSettings settings2 = new("lock2", "connection");

        settings1.Equals(settings2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenConnectionIsDifferent()
    {
        PostgreSqlLockSettings settings1 = new("lock", "connection1");
        PostgreSqlLockSettings settings2 = new("lock", "connection2");

        settings1.Equals(settings2).Should().BeFalse();
    }
}
