﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Background;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Tests.Core.TestTypes.Database;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Background
{
    public sealed class RecurringDistributedBackgroundServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        private readonly IServiceProvider _serviceProvider;

        public RecurringDistributedBackgroundServiceTests()
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
        [Trait("CI", "false")]
        public async Task StartAsync_WithDbLockManager_TaskIsExecuted()
        {
            bool executed = false;

            using TestRecurringDistributedBackgroundService service = new(
                _ =>
                {
                    executed = true;
                    return Task.CompletedTask;
                },
                _serviceProvider.GetRequiredService<DbDistributedLockManager>());
            await service.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed);

            executed.Should().BeTrue();
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task StartAsync_WithNullLockManager_TaskIsExecuted()
        {
            bool executed = false;

            using TestRecurringDistributedBackgroundService service = new(
                _ =>
                {
                    executed = true;
                    return Task.CompletedTask;
                },
                new NullLockManager());
            await service.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed);

            executed.Should().BeTrue();
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task StartAsync_WithDbLockManager_OnlyOneTaskIsExecutedSimultaneously()
        {
            bool executed1 = false;
            bool executed2 = false;

            using TestRecurringDistributedBackgroundService service1 = new(
                _ =>
                {
                    executed1 = true;
                    return Task.CompletedTask;
                },
                _serviceProvider.GetRequiredService<DbDistributedLockManager>());
            await service1.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed1);

            using TestRecurringDistributedBackgroundService service2 = new(
                _ =>
                {
                    executed2 = true;
                    return Task.CompletedTask;
                },
                _serviceProvider.GetRequiredService<DbDistributedLockManager>());
            await service2.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executed2, TimeSpan.FromMilliseconds(100));

            executed1.Should().BeTrue();
            executed2.Should().BeFalse();

            await service1.StopAsync(CancellationToken.None);
            await AsyncTestingUtil.WaitAsync(() => executed2);

            executed2.Should().BeTrue();
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task StartAsync_SimpleTask_TaskExecutedMultipleTimes()
        {
            int executions = 0;

            using TestRecurringDistributedBackgroundService service = new(
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                _serviceProvider.GetRequiredService<DbDistributedLockManager>());
            await service.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executions > 1);

            executions.Should().BeGreaterThan(1);
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task StopAsync_SimpleTask_ExecutionStopped()
        {
            int executions = 0;

            using TestRecurringDistributedBackgroundService service = new(
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                _serviceProvider.GetRequiredService<DbDistributedLockManager>());
            await service.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executions > 1);

            executions.Should().BeGreaterThan(0);

            await service.StopAsync(CancellationToken.None);
            await Task.Delay(50);
            int executionsBeforeStop = executions;

            await Task.Delay(500);

            executions.Should().Be(executionsBeforeStop);
        }

        [Fact]
        [Trait("CI", "false")]
        public async Task PauseAndResume_SimpleTask_ExecutionPausedAndResumed()
        {
            int executions = 0;

            using TestRecurringDistributedBackgroundService service = new(
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                _serviceProvider.GetRequiredService<DbDistributedLockManager>());
            await service.StartAsync(CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => executions > 1);

            executions.Should().BeGreaterThan(0);

            service.Pause();
            await Task.Delay(50);
            int executionsBeforeStop = executions;

            await Task.Delay(500);

            executions.Should().Be(executionsBeforeStop);

            service.Resume();

            await AsyncTestingUtil.WaitAsync(() => executions > executionsBeforeStop);

            executions.Should().BeGreaterThan(executionsBeforeStop);
        }

        public void Dispose()
        {
            _connection.SafeClose();
            _connection.Dispose();
        }

        private sealed class TestRecurringDistributedBackgroundService : RecurringDistributedBackgroundService
        {
            private readonly Func<CancellationToken, Task> _task;

            public TestRecurringDistributedBackgroundService(
                Func<CancellationToken, Task> task,
                IDistributedLockManager lockManager)
                : base(
                    TimeSpan.FromMilliseconds(100),
                    new DistributedLockSettings(
                        "test",
                        "unique",
                        TimeSpan.FromMilliseconds(500),
                        TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromMilliseconds(100)),
                    lockManager,
                    Substitute.For<ISilverbackLogger<RecurringDistributedBackgroundService>>())
            {
                _task = task;
            }

            protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
                _task.Invoke(stoppingToken);
        }
    }
}
