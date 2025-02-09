// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Background;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Background;

public class RecurringDistributedBackgroundServiceFixture
{
    [Fact]
    public async Task StartAsync_ShouldExecuteJob_WhenNoLockIsUsed()
    {
        bool executed = false;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        using TestRecurringDistributedBackgroundService service = new(
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(null, serviceProvider));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_ShouldExecuteJob_WhenInMemoryLockIsUsed()
    {
        bool executed = false;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .UseInMemoryLock());
        IDistributedLockFactory lockFactory = serviceProvider
            .GetRequiredService<IDistributedLockFactory>();

        using TestRecurringDistributedBackgroundService service = new(
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(new InMemoryLockSettings("lock"), serviceProvider));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_ShouldNotExecuteInParallel_WhenInMemoryLockIsUsed()
    {
        bool executed1 = false;
        bool executed2 = false;
        int executingCount = 0;
        bool executedInParallel = false;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .UseInMemoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        using TestRecurringDistributedBackgroundService service1 = new(
            async stoppingToken => await ExecuteTask(stoppingToken, () => executed1 = true),
            lockFactory.GetDistributedLock(new InMemoryLockSettings("shared-lock"), serviceProvider));
        using TestRecurringDistributedBackgroundService service2 = new(
            async stoppingToken => await ExecuteTask(stoppingToken, () => executed2 = true),
            lockFactory.GetDistributedLock(new InMemoryLockSettings("shared-lock"), serviceProvider));

        service2.DistributedLock.ShouldBeSameAs(service1.DistributedLock);

        async Task ExecuteTask(CancellationToken stoppingToken, Action execute)
        {
            Interlocked.Increment(ref executingCount);

            execute.Invoke();

            if (executingCount > 1)
                executedInParallel = true;

            await Task.Delay(100, stoppingToken);
            Interlocked.Decrement(ref executingCount);
        }

        await service1.StartAsync(CancellationToken.None);
        await service2.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed1 || executed2);
        await Task.Delay(100);

        (executed1 || executed2).ShouldBeTrue();
        (executed1 && executed2).ShouldBeFalse();

        if (executed1)
            await service1.StopAsync(CancellationToken.None);
        else
            await service2.StopAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed1 && executed2);
        (executed1 && executed2).ShouldBeTrue();

        executedInParallel.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldExecuteJobAgainAfterDelay()
    {
        DateTime? firstExecution = null;
        DateTime? secondExecution = null;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        using TestRecurringDistributedBackgroundService service = new(
            _ =>
            {
                if (firstExecution == null)
                    firstExecution = DateTime.UtcNow;
                else if (secondExecution == null)
                    secondExecution = DateTime.UtcNow;

                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(null, serviceProvider));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => secondExecution != null);

        firstExecution.ShouldNotBeNull();
        secondExecution.ShouldNotBeNull();
        (secondExecution.Value - firstExecution.Value).ShouldBeGreaterThan(TimeSpan.FromMilliseconds(90));
    }

    [Fact]
    public async Task StopAsync_ShouldStopExecution()
    {
        int executions = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        using TestRecurringDistributedBackgroundService service = new(
            _ =>
            {
                executions++;
                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(null, serviceProvider));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executions > 1);
        executions.ShouldBeGreaterThan(0);

        await service.StopAsync(CancellationToken.None);

        int executionsBeforeStop = executions;
        await Task.Delay(100);

        executions.ShouldBe(executionsBeforeStop);
    }

    [Fact]
    public async Task PauseAndResume_ShouldPauseAndResumeExecution()
    {
        int executions = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback());
        IDistributedLockFactory lockFactory = serviceProvider
            .GetRequiredService<IDistributedLockFactory>();

        using TestRecurringDistributedBackgroundService service = new(
            _ =>
            {
                executions++;
                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(null, serviceProvider));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executions > 1);
        executions.ShouldBeGreaterThan(0);

        service.Pause();

        int executionsBeforePause = executions;
        await Task.Delay(100);
        executions.ShouldBe(executionsBeforePause);

        service.Resume();

        await AsyncTestingUtil.WaitAsync(() => executions > executionsBeforePause);
        executions.ShouldBeGreaterThan(executionsBeforePause);
    }

    private sealed class TestRecurringDistributedBackgroundService : RecurringDistributedBackgroundService
    {
        private readonly Func<CancellationToken, Task> _task;

        public TestRecurringDistributedBackgroundService(Func<CancellationToken, Task> task, IDistributedLock distributedLock)
            : base(
                TimeSpan.FromMilliseconds(100),
                distributedLock,
                Substitute.For<ISilverbackLogger<RecurringDistributedBackgroundService>>())
        {
            DistributedLock = distributedLock;
            _task = task;
        }

        public new IDistributedLock DistributedLock { get; }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => _task.Invoke(stoppingToken);
    }
}
