// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Configuration;

public class SilverbackBuilderEntityFrameworkExtensionsFixture
{
    [Fact]
    public void AddEntityFrameworkLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("lock", typeof(TestDbContext), GetDbContext),
            serviceProvider);

        distributedLock.Should().BeOfType<EntityFrameworkLock>();
    }

    private static DbContext GetDbContext(IServiceProvider serviceProvider, SilverbackContext? context = null) => new TestDbContext();

    private class TestDbContext : DbContext
    {
    }
}
