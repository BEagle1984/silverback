// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Configuration;

public class SilverbackBuilderPostgreSqlExtensionsTests
{
    [Fact]
    public void AddPostgreSqlAdvisoryLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddPostgreSqlAdvisoryLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("lock", "conn"),
            serviceProvider);

        distributedLock.ShouldBeOfType<PostgreSqlAdvisoryLock>();
    }

    [Fact]
    public void AddPostgreSqlTableLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddPostgreSqlTableLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("lock", "conn"),
            serviceProvider);

        distributedLock.ShouldBeOfType<PostgreSqlTableLock>();
    }

    [Fact]
    public void AddPostgreSqlAdvisoryLock_ShouldThrow_WhenAddSilverbackHasNotBeenCalled()
    {
        SilverbackBuilder builder = new(new ServiceCollection());

        Action act = () => builder.AddPostgreSqlAdvisoryLock();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("DistributedLockFactory not found, AddSilverback has not been called.");
    }

    [Fact]
    public void AddPostgreSqlTableLock_ShouldThrow_WhenAddSilverbackHasNotBeenCalled()
    {
        SilverbackBuilder builder = new(new ServiceCollection());

        Action act = () => builder.AddPostgreSqlTableLock();

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("DistributedLockFactory not found, AddSilverback has not been called.");
    }

    [Fact]
    public void AddPostgreSqlAdvisoryLock_ShouldKeepRegisteredFactory_WhenFactoryIsAlreadyRegistered()
    {
        IDistributedLock customLock = Substitute.For<IDistributedLock>();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services =>
        {
            SilverbackBuilder builder = services.AddFakeLogger().AddSilverback();
            GetLockFactory(services).AddFactory<PostgreSqlAdvisoryLockSettings>((_, _) => customLock);
            builder.AddPostgreSqlAdvisoryLock();
        });
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("lock", "conn"),
            serviceProvider);

        distributedLock.ShouldBeSameAs(customLock);
    }

    [Fact]
    public void AddPostgreSqlTableLock_ShouldKeepRegisteredFactory_WhenFactoryIsAlreadyRegistered()
    {
        IDistributedLock customLock = Substitute.For<IDistributedLock>();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services =>
        {
            SilverbackBuilder builder = services.AddFakeLogger().AddSilverback();
            GetLockFactory(services).AddFactory<PostgreSqlTableLockSettings>((_, _) => customLock);
            builder.AddPostgreSqlTableLock();
        });
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("lock", "conn"),
            serviceProvider);

        distributedLock.ShouldBeSameAs(customLock);
    }

    private static DistributedLockFactory GetLockFactory(IServiceCollection services) =>
        services.Single(descriptor => descriptor.ServiceType == typeof(DistributedLockFactory))
            .ImplementationInstance.ShouldBeOfType<DistributedLockFactory>();
}
