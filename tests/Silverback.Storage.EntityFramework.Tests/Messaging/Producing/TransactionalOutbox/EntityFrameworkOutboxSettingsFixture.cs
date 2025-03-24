// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
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

        settings.DbContextType.ShouldBe(typeof(TestDbContext));
        settings.GetDbContext(Substitute.For<IServiceProvider>()).ShouldBeOfType<TestDbContext>();
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        EntityFrameworkOutboxSettings outboxSettings = new(typeof(TestDbContext), GetDbContext);

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        EntityFrameworkLockSettings efLockSettings = lockSettings.ShouldBeOfType<EntityFrameworkLockSettings>();
        efLockSettings.LockName.ShouldBe("outbox.TestDbContext");
        efLockSettings.DbContextType.ShouldBe(typeof(TestDbContext));
        efLockSettings.GetDbContext(Substitute.For<IServiceProvider>()).ShouldBeOfType<TestDbContext>();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        EntityFrameworkOutboxSettings outboxSettings = new(typeof(TestDbContext), GetDbContext);

        Action act = outboxSettings.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextTypeIsNull()
    {
        EntityFrameworkOutboxSettings settings = new(null!, GetDbContext);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The DbContext type is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenDbContextFactoryIsNull()
    {
        EntityFrameworkOutboxSettings settings = new(typeof(TestDbContext), null!);

        Action act = settings.Validate;

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The DbContext factory is required.");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
