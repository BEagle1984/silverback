﻿// Copyright (c) 2020 Sergio Aquilini
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
    public class RecurringDistributedBackgroundServiceTests
    {
        private readonly IServiceProvider _servicesProvider;

        public RecurringDistributedBackgroundServiceTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<TestDbContext>(opt => opt
                .UseInMemoryDatabase("TestDbContext"));
            services.AddSilverback().UseDbContext<TestDbContext>();

            _servicesProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task StartAsync_WithDbLockManager_TaskIsExecuted()
        {
            bool executed = false;

            var service = new TestRecurringDistributedBackgroundService(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }, new DbDistributedLockManager(_servicesProvider));
            await service.StartAsync(CancellationToken.None);

            AsyncTestingUtil.Wait(() => executed);

            executed.Should().BeTrue();
        }

        [Fact]
        public async Task StartAsync_WithDbLockManager_OnlyOneTaskIsExecutedSimultaneously()
        {
            bool executed1 = false;
            bool executed2 = false;

            var service1 = new TestRecurringDistributedBackgroundService(stoppingToken =>
            {
                executed1 = true;
                return Task.CompletedTask;
            }, new DbDistributedLockManager(_servicesProvider));
            await service1.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed1);

            var service2 = new TestRecurringDistributedBackgroundService(_ =>
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

        [Fact]
        public async Task StartAsync_SimpleTask_TaskExecutedMultipleTimes()
        {
            int executions = 0;

            var service = new TestRecurringDistributedBackgroundService(_ =>
            {
                executions++;
                return Task.CompletedTask;
            }, new DbDistributedLockManager(_servicesProvider));
            await service.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executions > 1);

            executions.Should().BeGreaterThan(1);
        }

        [Fact]
        public async Task StopAsync_SimpleTask_ExecutionStopped()
        {
            int executions = 0;

            var service = new TestRecurringDistributedBackgroundService(_ =>
            {
                executions++;
                return Task.CompletedTask;
            }, new DbDistributedLockManager(_servicesProvider));
            await service.StartAsync(CancellationToken.None);

            await Task.Delay(100);

            await service.StopAsync(CancellationToken.None);
            var executionsBeforeStop = executions;

            await Task.Delay(200);

            executions.Should().Be(executionsBeforeStop);
        }

        public class TestRecurringDistributedBackgroundService : RecurringDistributedBackgroundService
        {
            private readonly Func<CancellationToken, Task> _task;

            public TestRecurringDistributedBackgroundService(
                Func<CancellationToken, Task> task,
                IDistributedLockManager lockManager)
                : base(
                    TimeSpan.FromMilliseconds(10),
                    new DistributedLockSettings(
                        "test",
                        "unique",
                        TimeSpan.FromMilliseconds(500),
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromMilliseconds(100)),
                    lockManager,
                    Substitute.For<ILogger<RecurringDistributedBackgroundService>>())
            {
                _task = task;
            }

            protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
                _task.Invoke(stoppingToken);
        }
    }
}