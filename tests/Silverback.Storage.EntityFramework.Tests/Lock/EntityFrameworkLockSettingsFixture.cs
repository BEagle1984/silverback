// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Lock;

public class EntityFrameworkLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockNameAndContextTypeAndFactory()
    {
        EntityFrameworkLockSettings settings = new("my-lock", typeof(TestDbContext), GetDbContext);

        settings.LockName.ShouldBe("my-lock");
        settings.DbContextType.ShouldBe(typeof(TestDbContext));
        settings.GetDbContext(null!).ShouldBeOfType<TestDbContext>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        EntityFrameworkLockSettings settings = new("my-lock", typeof(TestDbContext), GetDbContext);

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        EntityFrameworkLockSettings settings = new(lockName!, typeof(TestDbContext), GetDbContext);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The lock name is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextTypeIsNull()
    {
        EntityFrameworkLockSettings settings = new("my-lock", null!, GetDbContext);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The DbContext type is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextFactoryIsNull()
    {
        EntityFrameworkLockSettings settings = new("my-lock", typeof(TestDbContext), null!);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The DbContext factory is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenAcquireIntervalIsZero()
    {
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
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
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
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
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
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
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
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
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
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
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
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
        EntityFrameworkLockSettings outboxSettings = new("my-lock", typeof(TestDbContext), null!)
        {
            LockTimeout = TimeSpan.FromSeconds(1),
            HeartbeatInterval = TimeSpan.FromSeconds(2)
        };

        Action act = outboxSettings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The lock timeout must be greater than the heartbeat interval.");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
