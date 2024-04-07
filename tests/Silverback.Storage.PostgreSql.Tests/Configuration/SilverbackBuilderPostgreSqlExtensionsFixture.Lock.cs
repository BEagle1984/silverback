// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Configuration;

public class SilverbackBuilderPostgreSqlExtensionsFixture
{
    [Fact]
    public void AddPostgreSqlAdvisoryLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlAdvisoryLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("lock", "conn"),
            serviceProvider);

        distributedLock.Should().BeOfType<PostgreSqlAdvisoryLock>();
    }

    [Fact]
    public void AddPostgreSqlTableLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlTableLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("lock", "conn"),
            serviceProvider);

        distributedLock.Should().BeOfType<PostgreSqlTableLock>();
    }
}
