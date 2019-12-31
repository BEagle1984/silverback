// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Background;
using Silverback.Tests.Core.TestTypes;
using Silverback.Tests.Core.TestTypes.Database;
using Xunit;

namespace Silverback.Tests.Core.Background
{
    public class DistributedBackgroundServiceTests
    {
        private readonly IServiceProvider _servicesProvider;

        public DistributedBackgroundServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<TestDbContext>(opt => opt
                .UseInMemoryDatabase("TestDbContext"));
            services.AddSilverback().UseDbContext<TestDbContext>();

            _servicesProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task StartAsync_NullLockManager_TaskIsExecuted()
        {
            bool executed = false;

            var service = new TestDistributedBackgroundService(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }, new NullLockManager());
            await service.StartAsync(CancellationToken.None);

            AsyncTestingUtil.Wait(() => executed);

            executed.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_WithDbLockManager_TaskIsExecuted()
        {
            bool executed = false;

            var service = new TestDistributedBackgroundService(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }, new DbDistributedLockManager(_servicesProvider));
            await service.StartAsync(CancellationToken.None);

            AsyncTestingUtil.Wait(() => executed);

            executed.Should().BeTrue();
        }

        [Fact, Trait("CI", "false")]
        public async Task StartAsync_WithDbLockManager_OnlyOneTaskIsExecutedSimultaneously()
        {
            bool executed1 = false;
            bool executed2 = false;

            var service1 = new TestDistributedBackgroundService(async stoppingToken =>
            {
                executed1 = true;

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(10, stoppingToken);
                }
            }, new DbDistributedLockManager(_servicesProvider));
            await service1.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed1);

            var service2 = new TestDistributedBackgroundService(_ =>
            {
                executed2 = true;
                return Task.CompletedTask;
            }, new DbDistributedLockManager(_servicesProvider));
            await service2.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed2, 100);

            executed1.Should().BeTrue();
            executed2.Should().BeFalse();

            await service1.StopAsync(CancellationToken.None);
            await AsyncTestingUtil.WaitAsync(() => executed2);

            executed2.Should().BeTrue();
        }

        public class TestDistributedBackgroundService : DistributedBackgroundService
        {
            private readonly Func<CancellationToken, Task> _task;

            public TestDistributedBackgroundService(
                Func<CancellationToken, Task> task,
                IDistributedLockManager lockManager)
                : base(
                    new DistributedLockSettings(
                        "test",
                        "unique",
                        TimeSpan.FromMilliseconds(500),
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromMilliseconds(100)),
                    lockManager,
                    Substitute.For<ILogger<DistributedBackgroundService>>())
            {
                _task = task;
            }

            protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => _task.Invoke(stoppingToken);
        }
    }
}