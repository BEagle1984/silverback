// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Storage;
using Silverback.Storage.DataAccess;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.PostgreSql.Lock;

public class PostgreSqlTableLockFixture : PostgresContainerFixture
{
    private readonly PostgreSqlTableLockSettings _lockSettings;

    public PostgreSqlTableLockFixture()
    {
        _lockSettings = new PostgreSqlTableLockSettings("test-lock", ConnectionString)
        {
            AcquireInterval = TimeSpan.FromMilliseconds(10),
            HeartbeatInterval = TimeSpan.FromMilliseconds(10),
            LockTimeout = TimeSpan.FromSeconds(100)
        };
    }

    [Fact]
    public async Task AcquireAsync_ShouldReturnHandle()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

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
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        IDistributedLock distributedLockA1 = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("A", ConnectionString),
            serviceProvider);
        IDistributedLock distributedLockA2 = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("A", ConnectionString),
            serviceProvider);
        IDistributedLock distributedLockB1 = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("B", ConnectionString),
            serviceProvider);
        IDistributedLock distributedLockB2 = lockFactory.GetDistributedLock(
            new PostgreSqlTableLockSettings("B", ConnectionString),
            serviceProvider);

        Task<DistributedLockHandle> taskA1 = distributedLockA1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskA2 = distributedLockA2.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB1 = distributedLockB1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB2 = distributedLockB2.AcquireAsync().AsTask();

        DistributedLockHandle handleA = await await Task.WhenAny(taskA1, taskA2);
        DistributedLockHandle handleB = await await Task.WhenAny(taskB1, taskB2);

        handleA.ShouldNotBeNull();
        handleB.ShouldNotBeNull();

        await Task.Delay(100);

        (taskA1.IsCompleted ^ taskA2.IsCompleted).ShouldBeTrue();
        (taskB1.IsCompleted ^ taskB2.IsCompleted).ShouldBeTrue();

        await handleA.DisposeAsync();
        handleB.Dispose();

        await Task.WhenAll(taskA1, taskA2, taskB1, taskB2);

        (taskA1.IsCompleted & taskA2.IsCompleted).ShouldBeTrue();
        (taskB1.IsCompleted & taskB2.IsCompleted).ShouldBeTrue();
    }

    [Fact]
    public async Task PostgreSqlTableLockHandle_Dispose_ShouldReleaseLock()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();
        handle.Dispose();

        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        handle = await distributedLock2.AcquireAsync();
        handle.ShouldNotBeNull();
    }

    [Fact]
    public async Task PostgreSqlTableLockHandle_Dispose_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.Dispose();
        Action act = handle.Dispose;

        act.ShouldNotThrow();
    }

    [Fact]
    public async Task PostgreSqlTableLockHandle_DisposeAsync_ShouldReleaseLock()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();
        await handle.DisposeAsync();

        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        handle = await distributedLock2.AcquireAsync();
        handle.ShouldNotBeNull();
    }

    [Fact]
    public async Task PostgreSqlTableLockHandle_DisposeAsync_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        await handle.DisposeAsync();
        Func<Task> act = () => handle.DisposeAsync().AsTask();

        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task PostgreSqlTableLockHandle_ShouldHeartbeat()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddPostgreSqlTableLock());

        SilverbackStorageInitializer storageInitializer = serviceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreatePostgreSqlLocksTableAsync(_lockSettings);

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        await using DistributedLockHandle handle = await distributedLock.AcquireAsync();

        PostgreSqlDataAccess dataAccess = new(ConnectionString);
        DateTime initialHeartbeat = dataAccess.ExecuteScalar<DateTime>($"SELECT LastHeartbeat FROM \"{_lockSettings.TableName}\" WHERE LockName = 'test-lock'", null, TimeSpan.FromSeconds(1));

        await Task.Delay(1000);

        DateTime lastHeartbeat = dataAccess.ExecuteScalar<DateTime>($"SELECT LastHeartbeat FROM \"{_lockSettings.TableName}\" WHERE LockName = 'test-lock'", null, TimeSpan.FromSeconds(1));
        lastHeartbeat.ShouldBeGreaterThan(initialHeartbeat);
    }
}
