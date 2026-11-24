// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Configuration;

public class SilverbackBuilderEntityFrameworkExtensionsTests
{
    [Fact]
    public void AddEntityFrameworkLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddEntityFrameworkLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("lock", typeof(TestDbContext), GetDbContext),
            serviceProvider);

        distributedLock.ShouldBeOfType<EntityFrameworkLock>();
    }

    [Fact]
    public void AddEntityFrameworkLock_ShouldNotThrow_WhenCalledTwice()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddEntityFrameworkLock()
            .AddEntityFrameworkLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("lock", typeof(TestDbContext), GetDbContext),
            serviceProvider);

        distributedLock.ShouldBeOfType<EntityFrameworkLock>();
    }

    [Fact]
    public void AddEntityFrameworkLock_ShouldThrow_WhenSilverbackHasNotBeenAdded()
    {
        SilverbackBuilder builder = new(new ServiceCollection());

        Action act = () => builder.AddEntityFrameworkLock();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("AddSilverback has not been called");
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext;
}
