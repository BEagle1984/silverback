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

namespace Silverback.Tests.Core.Background;

public sealed class DistributedBackgroundServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    private readonly IServiceProvider _serviceProvider;

    public DistributedBackgroundServiceTests()
    {
        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        ServiceCollection services = new();

        services
            .AddTransient<DbDistributedLockManager>()
            .AddDbContext<TestDbContext>(
                opt => opt
                    .UseSqlite(_connection.ConnectionString))
            .AddLoggerSubstitute()
            .AddSilverback()
            .UseDbContext<TestDbContext>();

        _serviceProvider = services.BuildServiceProvider();

        using IServiceScope scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task StartAsync_NullLockManager_TaskIsExecuted()
    {
        bool executed = false;

        using TestDistributedBackgroundService service = new(
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
    public async Task StartAsync_WithDbLockManager_TaskIsExecuted()
    {
        bool executed = false;

        using TestDistributedBackgroundService service = new(
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
    public async Task StartAsync_WithDbLockManager_OnlyOneTaskIsExecutedSimultaneously()
    {
        bool executed1 = false;
        bool executed2 = false;

        using TestDistributedBackgroundService service1 = new(
            async stoppingToken =>
            {
                executed1 = true;

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(10, stoppingToken);
                }
            },
            _serviceProvider.GetRequiredService<DbDistributedLockManager>());
        await service1.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed1);

        using TestDistributedBackgroundService service2 = new(
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

    public void Dispose()
    {
        _connection.SafeClose();
        _connection.Dispose();
    }

    private sealed class TestDistributedBackgroundService : DistributedBackgroundService
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
                Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
            _task = task;
        }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => _task.Invoke(stoppingToken);
    }
}
