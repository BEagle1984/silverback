// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Lock;

public class EntityFrameworkLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockNameAndContextTypeAndFactory()
    {
        EntityFrameworkLockSettings settings = new("my-lock", typeof(TestDbContext), GetDbContext);

        settings.LockName.Should().Be("my-lock");
        settings.DbContextType.Should().Be(typeof(TestDbContext));
        settings.GetDbContext(null!).Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        EntityFrameworkLockSettings settings = new("my-lock", typeof(TestDbContext), GetDbContext);

        Action act = settings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        EntityFrameworkLockSettings settings = new(lockName!, typeof(TestDbContext), GetDbContext);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The lock name is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextTypeIsNull()
    {
        EntityFrameworkLockSettings settings = new("my-lock", null!, GetDbContext);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The DbContext type is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextFactoryIsNull()
    {
        EntityFrameworkLockSettings settings = new("my-lock", typeof(TestDbContext), null!);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The DbContext factory is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAcquireIntervalIsZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            AcquireInterval = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The acquire interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAcquireIntervalIsLessThanZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            AcquireInterval = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The acquire interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenHeartbeatIntervalIsZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            HeartbeatInterval = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The heartbeat interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenHeartbeatIntervalIsLessThanZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            HeartbeatInterval = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The heartbeat interval must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLockTimeoutIsZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            LockTimeout = TimeSpan.Zero
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The lock timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLockTimeoutIsLessThanZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            LockTimeout = TimeSpan.FromSeconds(-1)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The lock timeout must be greater than zero.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLockTimeoutIsLessThanHeartbeatInterval()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            LockTimeout = TimeSpan.FromSeconds(1),
            HeartbeatInterval = TimeSpan.FromSeconds(2)
        };

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The lock timeout must be greater than the heartbeat interval.");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, SilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
