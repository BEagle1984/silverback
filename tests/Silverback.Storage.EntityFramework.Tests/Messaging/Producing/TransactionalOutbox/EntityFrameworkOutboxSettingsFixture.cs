// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Messaging.Producing.TransactionalOutbox;

public class EntityFrameworkOutboxSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetConnectionStringWithDefaultTableName()
    {
        EntityFrameworkOutboxSettings settings = new(typeof(TestDbContext), GetDbContext);

        settings.DbContextType.Should().Be(typeof(TestDbContext));
        settings.GetDbContext(Substitute.For<IServiceProvider>()).Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        EntityFrameworkOutboxSettings outboxSettings = new(typeof(TestDbContext), GetDbContext);

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<EntityFrameworkLockSettings>();
        lockSettings.As<EntityFrameworkLockSettings>().LockName.Should().Be("outbox.TestDbContext");
        lockSettings.As<EntityFrameworkLockSettings>().DbContextType.Should().Be(typeof(TestDbContext));
        lockSettings.As<EntityFrameworkLockSettings>().GetDbContext(Substitute.For<IServiceProvider>())
            .Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        EntityFrameworkOutboxSettings outboxSettings = new(typeof(TestDbContext), GetDbContext);

        Action act = outboxSettings.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextTypeIsNull()
    {
        EntityFrameworkOutboxSettings settings = new(null!, GetDbContext);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The DbContext type is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextFactoryIsNull()
    {
        EntityFrameworkOutboxSettings settings = new(typeof(TestDbContext), null!);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The DbContext factory is required.");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
