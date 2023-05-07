// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlLockFixture : PostgresContainerFixture
{
    [Fact]
    public async Task AcquireAsync_ShouldReturnHandle()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("Lock", ConnectionString));

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.Should().NotBeNull();
        handle.LockLostToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public async Task PostgreSqlLockHandle_Dispose_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("Lock", ConnectionString));

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.Dispose();
        Action act = handle.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task PostgreSqlLockHandle_DisposeAsync_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("Lock2", ConnectionString));

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        await handle.DisposeAsync();
        Func<Task> act = () => handle.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AcquireAsync_ShouldGrantExclusiveLockByName()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddPostgreSqlLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        IDistributedLock distributedLockA1 = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("A", ConnectionString));
        IDistributedLock distributedLockA2 = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("A", ConnectionString));
        IDistributedLock distributedLockB1 = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("B", ConnectionString));
        IDistributedLock distributedLockB2 = lockFactory.GetDistributedLock(new PostgreSqlLockSettings("B", ConnectionString));

        Task<DistributedLockHandle> taskA1 = distributedLockA1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskA2 = distributedLockA2.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB1 = distributedLockB1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB2 = distributedLockB2.AcquireAsync().AsTask();

        DistributedLockHandle handleA = await await Task.WhenAny(taskA1, taskA2);
        DistributedLockHandle handleB = await await Task.WhenAny(taskB1, taskB2);

        handleA.Should().NotBeNull();
        handleB.Should().NotBeNull();

        await Task.Delay(50);

        (taskA1.IsCompleted ^ taskA2.IsCompleted).Should().BeTrue();
        (taskB1.IsCompleted ^ taskB2.IsCompleted).Should().BeTrue();

        await handleA.DisposeAsync();
        handleB.Dispose();

        await Task.WhenAll(taskA1, taskA2, taskB1, taskB2);

        (taskA1.IsCompleted & taskA2.IsCompleted).Should().BeTrue();
        (taskB1.IsCompleted & taskB2.IsCompleted).Should().BeTrue();
    }
}
