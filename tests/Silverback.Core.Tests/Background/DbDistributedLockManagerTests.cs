// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Background;
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
            var distributedLockSettings = new DistributedLockSettings();
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
        public async Task Acquire_DefaultArguments_LockIsAcquired()
        {
            var distributedLock = await new DbDistributedLockManager(_servicesProvider)
                .Acquire("test.resource");

            distributedLock.Should().NotBeNull();
        }

        [Fact]
        public async Task Acquire_DefaultArguments_LockIsWrittenToDb()
        {
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire("test.resource");

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(1);
            dbContext.Locks.Single().Name.Should().Be("test.resource");
        }

        [Fact]
        public async Task Acquire_ResourceAlreadyLocked_TimeoutExceptionIsThrown()
        {
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire("test.resource", TimeSpan.FromMilliseconds(100));

            Func<Task> act = () => new DbDistributedLockManager(_servicesProvider)
                .Acquire("test.resource", TimeSpan.FromMilliseconds(100));

            act.Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public async Task Release_LockedResource_LockIsRemoved()
        {
            await new DbDistributedLockManager(_servicesProvider)
                .Acquire("test.resource");

            await new DbDistributedLockManager(_servicesProvider).Release("test.resource");

            var dbContext = GetDbContext();
            dbContext.Locks.Count().Should().Be(0);
        }

        private TestDbContext GetDbContext() =>
            _servicesProvider.CreateScope().ServiceProvider.GetService<TestDbContext>();
    }
}