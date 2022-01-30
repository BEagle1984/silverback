// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Background;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Background;

public class DistributedBackgroundServiceFixture
{
    [Fact]
    public async Task StartAsync_ShouldExecuteJob_WhenNoLockIsUsed()
    {
        bool executed = false;

        IDistributedLockFactory lockFactory = ServiceProviderHelper
            .GetServiceProvider(services => services.AddFakeLogger().AddSilverback())
            .GetRequiredService<IDistributedLockFactory>();

        using TestDistributedBackgroundService service = new(
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(null));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed);
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_ShouldExecuteJob_WhenInMemoryLockIsUsed()
    {
        bool executed = false;

        IDistributedLockFactory lockFactory = ServiceProviderHelper
            .GetServiceProvider(services => services.AddFakeLogger().AddSilverback().UseInMemoryLock())
            .GetRequiredService<IDistributedLockFactory>();

        using TestDistributedBackgroundService service = new(
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            lockFactory.GetDistributedLock(new InMemoryLockSettings("lock")));
        await service.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed);
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task StartAsync_ShouldNotExecuteInParallel_WhenInMemoryLockIsUsed()
    {
        bool executed1 = false;
        bool executed2 = false;
        int executingCount = 0;
        bool executedInParallel = false;

        IDistributedLockFactory lockFactory = ServiceProviderHelper
            .GetServiceProvider(services => services.AddFakeLogger().AddSilverback().UseInMemoryLock())
            .GetRequiredService<IDistributedLockFactory>();

        using TestDistributedBackgroundService service1 = new(
            async stoppingToken => await ExecuteTask(stoppingToken, () => executed1 = true),
            lockFactory.GetDistributedLock(new InMemoryLockSettings("shared-lock")));
        using TestDistributedBackgroundService service2 = new(
            async stoppingToken => await ExecuteTask(stoppingToken, () => executed2 = true),
            lockFactory.GetDistributedLock(new InMemoryLockSettings("shared-lock")));

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

        await AsyncTestingUtil.WaitAsync(() => executed1 && executed2);

        executed1.Should().BeTrue();
        executed2.Should().BeTrue();
        executedInParallel.Should().BeFalse();
    }

    private sealed class TestDistributedBackgroundService : DistributedBackgroundService
    {
        private readonly Func<CancellationToken, Task> _task;

        public TestDistributedBackgroundService(Func<CancellationToken, Task> task, IDistributedLock distributedLock)
            : base(distributedLock, Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
            _task = task;
        }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => _task.Invoke(stoppingToken);
    }
}
