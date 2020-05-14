// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Background;
using Silverback.Background.Model;
using Silverback.Tests.Core.TestTypes.Database;
using Xunit;

namespace Silverback.Tests.Core.Background
{
    public sealed class DbDistributedLockManagerTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        private readonly IServiceProvider _serviceProvider;

        public DbDistributedLockManagerTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var services = new ServiceCollection();

            services
                .AddTransient<DbDistributedLockManager>()
                .AddDbContext<TestDbContext>(
                    options => options
                        .UseSqlite(_connection))
                .AddNullLogger()
                .AddSilverback()
                .UseDbContext<TestDbContext>();

            _serviceProvider = services.BuildServiceProvider();

            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
        }

        [Fact]
        public async Task Acquire_DefaultLockSettings_LockIsAcquired()
        {
            var distributedLockSettings = new DistributedLockSettings("test.resource");
            var distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(distributedLockSettings);

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_DefaultLockSettings_LockIsWrittenToDb()
        {
            var distributedLockSettings = new DistributedLockSettings("test.resource");
            await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(distributedLockSettings);

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(1);
            dbContext.Locks.Single().Name.Should().Be("test.resource");
        }

        [Fact]
        public async Task Acquire_NullLockSettings_NoLockIsAcquired()
        {
            var distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(DistributedLockSettings.NoLock);

            distributedLock.Should().BeNull();
        }

        [Fact]
        public async Task Acquire_NullLockSettings_LockIsNotWrittenToDb()
        {
            await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(DistributedLockSettings.NoLock);

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(0);
        }

        [Fact]
        public async Task Acquire_ExistingExpiredLockWithSameUniqueId_LockIsAcquired()
        {
            var db = GetDbContext();
            db.Locks.Add(
                new Lock
                {
                    Name = "test.resource",
                    UniqueId = "unique",
                    Created = DateTime.UtcNow.AddHours(-2),
                    Heartbeat = DateTime.UtcNow.AddHours(-1)
                });
            db.SaveChanges();

            var distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(new DistributedLockSettings("test.resource", "unique"));

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_ExistingExpiredLockWithDifferentUniqueId_LockIsAcquired()
        {
            var db = GetDbContext();
            db.Locks.Add(
                new Lock
                {
                    Name = "test.resource",
                    UniqueId = "other",
                    Created = DateTime.UtcNow.AddHours(-2),
                    Heartbeat = DateTime.UtcNow.AddHours(-1)
                });
            db.SaveChanges();

            var distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(new DistributedLockSettings("test.resource", "unique"));

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_ExistingExpiredLock_LockIsWrittenToDb()
        {
            var db = GetDbContext();
            db.Locks.Add(
                new Lock
                {
                    Name = "test.resource",
                    Created = DateTime.UtcNow.AddHours(-2),
                    Heartbeat = DateTime.UtcNow.AddHours(-1)
                });
            db.SaveChanges();

            await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(new DistributedLockSettings("test.resource", "unique"));

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(1);
            dbContext.Locks.Single().Name.Should().Be("test.resource");
            dbContext.Locks.Single().Created.Should().BeAfter(DateTime.UtcNow.AddSeconds(-2));
        }

        [Fact]
        public async Task Acquire_ResourceAlreadyLocked_TimeoutExceptionIsThrown()
        {
            await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(
                    new DistributedLockSettings(
                        "test.resource",
                        "unique",
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromMilliseconds(100)));

            Func<Task> act = () => _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(
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
                .Acquire(new DistributedLockSettings("test.resource", "unique", TimeSpan.FromMilliseconds(100)));

            var distributedLock = await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(DistributedLockSettings.NoLock);

            distributedLock.Should().BeNull();
        }

        [Fact]
        public async Task Acquire_Concurrency_OneAndOnlyOneLockIsAcquired()
        {
            var tasks = Enumerable.Range(1, 2)
                .Select(
                    async _ =>
                    {
                        try
                        {
                            return await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                                .Acquire(
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

            var results = await Task.WhenAll(tasks);

            results.Count(x => x != null).Should().Be(1);
        }

        [Fact]
        public async Task Release_LockedResource_LockIsRemoved()
        {
            var settings = new DistributedLockSettings("test.resource", "unique");
            await _serviceProvider.GetRequiredService<DbDistributedLockManager>()
                .Acquire(settings);

            await _serviceProvider.GetRequiredService<DbDistributedLockManager>().Release(settings);

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(0);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        private TestDbContext GetDbContext() =>
            _serviceProvider.CreateScope().ServiceProvider.GetService<TestDbContext>();
    }
}
