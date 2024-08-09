// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Lock;

public sealed class EntityFrameworkLockFixture : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    private readonly Type _dbContextType;

    private readonly Func<IServiceProvider, ISilverbackContext?, DbContext> _dbContextFactory;

    private readonly EntityFrameworkLockSettings _lockSettings;

    public EntityFrameworkLockFixture()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        _dbContextType = typeof(TestDbContext);
        _dbContextFactory = (serviceProvider, _) => serviceProvider.GetRequiredService<TestDbContext>();
        _lockSettings = new EntityFrameworkLockSettings("test-lock", _dbContextType, _dbContextFactory)
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
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.Should().NotBeNull();
        handle.LockLostToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireAsync_ShouldGrantExclusiveLockByName()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        IDistributedLock distributedLockA1 = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("A", _dbContextType, _dbContextFactory),
            serviceProvider);
        IDistributedLock distributedLockA2 = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("A", _dbContextType, _dbContextFactory),
            serviceProvider);
        IDistributedLock distributedLockB1 = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("B", _dbContextType, _dbContextFactory),
            serviceProvider);
        IDistributedLock distributedLockB2 = lockFactory.GetDistributedLock(
            new EntityFrameworkLockSettings("B", _dbContextType, _dbContextFactory),
            serviceProvider);

        Task<DistributedLockHandle> taskA1 = distributedLockA1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskA2 = distributedLockA2.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB1 = distributedLockB1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB2 = distributedLockB2.AcquireAsync().AsTask();

        DistributedLockHandle handleA = await await Task.WhenAny(taskA1, taskA2);
        DistributedLockHandle handleB = await await Task.WhenAny(taskB1, taskB2);

        handleA.Should().NotBeNull();
        handleB.Should().NotBeNull();

        await Task.Delay(100);

        (taskA1.IsCompleted ^ taskA2.IsCompleted).Should().BeTrue();
        (taskB1.IsCompleted ^ taskB2.IsCompleted).Should().BeTrue();

        await handleA.DisposeAsync();
        handleB.Dispose();

        await Task.WhenAll(taskA1, taskA2, taskB1, taskB2);

        (taskA1.IsCompleted & taskA2.IsCompleted).Should().BeTrue();
        (taskB1.IsCompleted & taskB2.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task EntityFrameworkTableLockHandle_Dispose_ShouldReleaseLock()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .EnableStorage()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();
        handle.Dispose();

        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        handle = await distributedLock2.AcquireAsync();
        handle.Should().NotBeNull();
    }

    [Fact]
    public async Task EntityFrameworkTableLockHandle_Dispose_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        handle.Dispose();
        Action act = handle.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task EntityFrameworkTableLockHandle_DisposeAsync_ShouldReleaseLock()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();
        await handle.DisposeAsync();

        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        handle = await distributedLock2.AcquireAsync();
        handle.Should().NotBeNull();
    }

    [Fact]
    public async Task EntityFrameworkTableLockHandle_DisposeAsync_ShouldNotThrowIfCalledMultipleTimes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        DistributedLockHandle handle = await distributedLock.AcquireAsync();

        await handle.DisposeAsync();
        Func<Task> act = () => handle.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EntityFrameworkTableLockHandle_ShouldHeartbeat()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddDbContext<TestDbContext>(options => options.UseSqlite(_sqliteConnection))
                .AddFakeLogger()
                .AddSilverback()
                .AddEntityFrameworkLock());

        using IServiceScope scope = serviceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
        IDistributedLock distributedLock = lockFactory.GetDistributedLock(_lockSettings, serviceProvider);

        await using DistributedLockHandle handle = await distributedLock.AcquireAsync();

        DateTime initialHeartbeat = dbContext.Locks.AsNoTracking().Single().LastHeartbeat ?? DateTime.MinValue;

        await AsyncTestingUtil.WaitAsync(() => dbContext.Locks.AsNoTracking().Single().LastHeartbeat > initialHeartbeat);

        DateTime lastHeartbeat = dbContext.Locks.AsNoTracking().Single().LastHeartbeat ?? DateTime.MinValue;
        lastHeartbeat.Should().BeAfter(initialHeartbeat);
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<SilverbackLock> Locks { get; set; } = null!;
    }
}
