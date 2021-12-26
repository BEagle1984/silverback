// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Background;
using Silverback.Configuration;
using Silverback.Database.Model;
using Silverback.Tests.Core.TestTypes.Database;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Background;

public sealed class DbDistributedLockManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;

    private readonly IServiceProvider _serviceProvider;

    public DbDistributedLockManagerTests()
    {
        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        ServiceCollection services = new();

        services
            .AddTransient<DbDistributedLockManager>()
            .AddDbContext<TestDbContext>(
                options => options
                    .UseSqlite(_connection.ConnectionString))
            .AddLoggerSubstitute()
            .AddSilverback()
            .UseDbContext<TestDbContext>();

        _serviceProvider = services.BuildServiceProvider();

        using IServiceScope scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task Acquire_DefaultLockSettings_LockIsAcquired()
    {
        DistributedLockSettings distributedLockSettings = new("test.resource");
        DistributedLock? distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(distributedLockSettings);

        distributedLock.Should().NotBeNull();
    }

    [Fact]
    public async Task Acquire_DefaultLockSettings_LockIsWrittenToDb()
    {
        DistributedLockSettings distributedLockSettings = new("test.resource");
        await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(distributedLockSettings);

        TestDbContext dbContext = GetDbContext();
        dbContext.Locks.Should().HaveCount(1);
        dbContext.Locks.Single().Name.Should().Be("test.resource");
    }

    [Fact]
    public async Task Acquire_NullLockSettings_NoLockIsAcquired()
    {
        DistributedLock? distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(DistributedLockSettings.NoLock);

        distributedLock.Should().BeNull();
    }

    [Fact]
    public async Task Acquire_NullLockSettings_LockIsNotWrittenToDb()
    {
        await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(DistributedLockSettings.NoLock);

        TestDbContext dbContext = GetDbContext();
        dbContext.Locks.Should().BeEmpty();
    }

    [Fact]
    public async Task Acquire_ExistingExpiredLockWithSameUniqueId_LockIsAcquired()
    {
        TestDbContext db = GetDbContext();
        db.Locks.Add(
            new Lock
            {
                Name = "test.resource",
                UniqueId = "unique",
                Created = DateTime.UtcNow.AddHours(-2),
                Heartbeat = DateTime.UtcNow.AddHours(-1)
            });
        db.SaveChanges();

        DistributedLock? distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(new DistributedLockSettings("test.resource", "unique"));

        distributedLock.Should().NotBeNull();
    }

    [Fact]
    public async Task Acquire_ExistingExpiredLockWithDifferentUniqueId_LockIsAcquired()
    {
        TestDbContext db = GetDbContext();
        db.Locks.Add(
            new Lock
            {
                Name = "test.resource",
                UniqueId = "other",
                Created = DateTime.UtcNow.AddHours(-2),
                Heartbeat = DateTime.UtcNow.AddHours(-1)
            });
        db.SaveChanges();

        DistributedLock? distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(new DistributedLockSettings("test.resource", "unique"));

        distributedLock.Should().NotBeNull();
    }

    [Fact]
    public async Task Acquire_ExistingExpiredLock_LockIsWrittenToDb()
    {
        TestDbContext db = GetDbContext();
        db.Locks.Add(
            new Lock
            {
                Name = "test.resource",
                Created = DateTime.UtcNow.AddHours(-2),
                Heartbeat = DateTime.UtcNow.AddHours(-1)
            });
        db.SaveChanges();

        await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(new DistributedLockSettings("test.resource", "unique"));

        TestDbContext dbContext = GetDbContext();
        dbContext.Locks.Should().HaveCount(1);
        dbContext.Locks.Single().Name.Should().Be("test.resource");
        dbContext.Locks.Single().Created.Should().BeAfter(DateTime.UtcNow.AddSeconds(-2));
    }

    [Fact]
    public async Task Acquire_ResourceAlreadyLocked_TimeoutExceptionIsThrown()
    {
        await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(
                new DistributedLockSettings(
                    "test.resource",
                    "unique",
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(100)));

        Func<Task> act = () => _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(
                new DistributedLockSettings(
                    "test.resource",
                    "unique",
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(100)));

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task Acquire_NullLockSettings_LockIgnoredButNoLockAcquired()
    {
        await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(new DistributedLockSettings("test.resource", "unique", TimeSpan.FromMilliseconds(100)));

        DistributedLock? distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(DistributedLockSettings.NoLock);

        distributedLock.Should().BeNull();
    }

    [Fact]
    public async Task Acquire_Concurrency_OneAndOnlyOneLockIsAcquired()
    {
        IEnumerable<Task<DistributedLock?>> tasks = Enumerable.Range(1, 2)
            .Select(
                async _ =>
                {
                    try
                    {
                        return await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                            .AcquireAsync(
                                new DistributedLockSettings(
                                    "test.resource",
                                    "unique",
                                    TimeSpan.FromMilliseconds(100),
                                    TimeSpan.FromMilliseconds(20)));
                    }
                    catch (TimeoutException)
                    {
                        return null;
                    }
                });

        DistributedLock?[] results = await Task.WhenAll(tasks);

        results.Count(x => x != null).Should().Be(1);
    }

    [Fact]
    public async Task Release_LockedResource_LockIsRemoved()
    {
        DistributedLockSettings settings = new("test.resource", "unique");
        await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
            .AcquireAsync(settings);

        await _serviceProvider.GetRequiredService<DbDistributedLockManager>().ReleaseAsync(settings);

        TestDbContext dbContext = GetDbContext();
        dbContext.Locks.Should().BeEmpty();
    }

    public void Dispose()
    {
        _connection.SafeClose();
        _connection.Dispose();
    }

    private TestDbContext GetDbContext() =>
        _serviceProvider.CreateScope().ServiceProvider.GetService<TestDbContext>() ??
        throw new InvalidOperationException("TestDbContext not resolved.");
}
