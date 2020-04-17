// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Background;
using Silverback.Background.Model;
using Silverback.Tests.Core.TestTypes.Database;
using Xunit;

namespace Silverback.Tests.Core.Background
{
    public class DbDistributedLockManagerTests
    {
        private readonly IServiceProvider _servicesProvider;

        public DbDistributedLockManagerTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<TestDbContext>(opt => opt
                .UseInMemoryDatabase("TestDbContext"));
            services.AddSilverback().UseDbContext<TestDbContext>();

            _servicesProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Acquire_DefaultLockSettings_LockIsAcquired()
        {
            var distributedLockSettings = new DistributedLockSettings("test.resource");
            var distributedLock = await new DbDistributedLockManager(_servicesProvider)
                .Acquire(distributedLockSettings);

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_DefaultLockSettings_LockIsWrittenToDb()
        {
            var distributedLockSettings = new DistributedLockSettings("test.resource");
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire(distributedLockSettings);

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(1);
            dbContext.Locks.Single().Name.Should().Be("test.resource");
        }
        
        [Fact]
        public async Task Acquire_NullLockSettings_NoLockIsAcquired()
        {
            var distributedLock = await new DbDistributedLockManager(_servicesProvider)
                .Acquire(DistributedLockSettings.NoLock);

            distributedLock.Should().BeNull();
        }
        
        [Fact]
        public async Task Acquire_NullLockSettings_LockIsNotWrittenToDb()
        {
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire(DistributedLockSettings.NoLock);

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(0);
        }

        [Fact]
        public async Task Acquire_ExistingExpiredLockWithSameUniqueId_LockIsAcquired()
        {
            var db = GetDbContext();
            db.Locks.Add(new Lock
            {
                Name = "test.resource",
                UniqueId = "unique",
                Created = DateTime.UtcNow.AddHours(-2),
                Heartbeat = DateTime.UtcNow.AddHours(-1)
            });
            db.SaveChanges();

            var distributedLock = await new DbDistributedLockManager(_servicesProvider)
                .Acquire(new DistributedLockSettings("test.resource", "unique"));

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_ExistingExpiredLockWithDifferentUniqueId_LockIsAcquired()
        {
            var db = GetDbContext();
            db.Locks.Add(new Lock
            {
                Name = "test.resource",
                UniqueId = "other",
                Created = DateTime.UtcNow.AddHours(-2),
                Heartbeat = DateTime.UtcNow.AddHours(-1)
            });
            db.SaveChanges();

            var distributedLock = await new DbDistributedLockManager(_servicesProvider)
                .Acquire(new DistributedLockSettings("test.resource", "unique"));

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_ExistingExpiredLock_LockIsWrittenToDb()
        {
            var db = GetDbContext();
            db.Locks.Add(new Lock
            {
                Name = "test.resource",
                Created = DateTime.UtcNow.AddHours(-2),
                Heartbeat = DateTime.UtcNow.AddHours(-1)
            });
            db.SaveChanges();

            await new DbDistributedLockManager(_servicesProvider)
                .Acquire(new DistributedLockSettings("test.resource", "unique"));

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(1);
            dbContext.Locks.Single().Name.Should().Be("test.resource");
            dbContext.Locks.Single().Created.Should().BeAfter(DateTime.UtcNow.AddSeconds(-2));
        }

        [Fact]
        public async Task Acquire_ResourceAlreadyLocked_TimeoutExceptionIsThrown()
        {
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire(new DistributedLockSettings("test.resource", "unique", TimeSpan.FromMilliseconds(100)));

            Func<Task> act = () => new DbDistributedLockManager(_servicesProvider)
                .Acquire(new DistributedLockSettings("test.resource", "unique", TimeSpan.FromMilliseconds(100)));

            await act.Should().ThrowAsync<TimeoutException>();
        }
        
        [Fact]
        public async Task Acquire_NullLockSettings_LockIgnoredButNoLockAcquired()
        {
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire(new DistributedLockSettings("test.resource", "unique", TimeSpan.FromMilliseconds(100)));

            var distributedLock = await new DbDistributedLockManager(_servicesProvider)
                .Acquire(DistributedLockSettings.NoLock);

            distributedLock.Should().BeNull();
        }

        [Fact]
        public async Task Acquire_Concurrency_OneAndOnlyOneLockIsAcquired()
        {
            var tasks = Enumerable.Range(1, 2)
                .Select(async _ =>
                {
                    try
                    {
                        return await new DbDistributedLockManager(_servicesProvider)
                            .Acquire(new DistributedLockSettings(
                                "test.resource",
                                "unique",
                                acquireTimeout: TimeSpan.FromMilliseconds(100),
                                acquireRetryInterval: TimeSpan.FromMilliseconds(20)));
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
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire(settings);

            await new DbDistributedLockManager(_servicesProvider).Release(settings);

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(0);
        }

        private TestDbContext GetDbContext() =>
            _servicesProvider.CreateScope().ServiceProvider.GetService<TestDbContext>();
    }
}