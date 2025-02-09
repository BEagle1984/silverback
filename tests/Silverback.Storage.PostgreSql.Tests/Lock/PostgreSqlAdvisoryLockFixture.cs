// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlAdvisoryLockFixture : PostgresContainerFixture
{
    [Fact]
    public async Task AcquireAsync_ShouldReturnHandle()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlAdvisoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock", ConnectionString),
            serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.ShouldNotBeNull();
        handle.LockLostToken.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public async Task AcquireAsync_ShouldGrantExclusiveLockByName()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlAdvisoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        IDistributedLock distributedLockA1 = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("A", ConnectionString),
            serviceProvider);
        IDistributedLock distributedLockA2 = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("A", ConnectionString),
            serviceProvider);
        IDistributedLock distributedLockB1 = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("B", ConnectionString),
            serviceProvider);
        IDistributedLock distributedLockB2 = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("B", ConnectionString),
            serviceProvider);

        Task<DistributedLockHandle> taskA1 = distributedLockA1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskA2 = distributedLockA2.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB1 = distributedLockB1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB2 = distributedLockB2.AcquireAsync().AsTask();

        DistributedLockHandle handleA = await await Task.WhenAny(taskA1, taskA2);
        DistributedLockHandle handleB = await await Task.WhenAny(taskB1, taskB2);

        handleA.ShouldNotBeNull();
        handleB.ShouldNotBeNull();

        await Task.Delay(50);

        (taskA1.IsCompleted ^ taskA2.IsCompleted).ShouldBeTrue();
        (taskB1.IsCompleted ^ taskB2.IsCompleted).ShouldBeTrue();

        await handleA.DisposeAsync();
        handleB.Dispose();

        await Task.WhenAll(taskA1, taskA2, taskB1, taskB2);

        (taskA1.IsCompleted & taskA2.IsCompleted).ShouldBeTrue();
        (taskB1.IsCompleted & taskB2.IsCompleted).ShouldBeTrue();
    }

    [Fact]
    public async Task PostgreSqlAdvisoryLockHandle_Dispose_ShouldReleaseLock()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlAdvisoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock", ConnectionString),
            serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();
        handle.Dispose();

        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock", ConnectionString),
            serviceProvider);

        handle = await distributedLock2.AcquireAsync();
        handle.ShouldNotBeNull();
    }

    [Fact]
    public async Task PostgreSqlAdvisoryLockHandle_Dispose_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlAdvisoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock", ConnectionString),
            serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.Dispose();
        Action act = handle.Dispose;

        act.ShouldNotThrow();
    }

    [Fact]
    public async Task PostgreSqlAdvisoryLockHandle_DisposeAsync_ShouldReleaseLock()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlAdvisoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock", ConnectionString),
            serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();
        await handle.DisposeAsync();

        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock", ConnectionString),
            serviceProvider);

        handle = await distributedLock2.AcquireAsync();
        handle.ShouldNotBeNull();
    }

    [Fact]
    public async Task PostgreSqlAdvisoryLockHandle_DisposeAsync_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlAdvisoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new PostgreSqlAdvisoryLockSettings("Lock2", ConnectionString),
            serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        await handle.DisposeAsync();
        Func<Task> act = () => handle.DisposeAsync().AsTask();

        await act.ShouldNotThrowAsync();
    }
}
