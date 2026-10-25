// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Silverback.Background;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Background;

public class DistributedBackgroundServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldExecuteJob_WhenNoLockIsUsed()
    {
        bool executed = false;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        using TestDistributedBackgroundService service = new(
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

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .UseInMemoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        using TestDistributedBackgroundService service = new(
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

        IServiceProvider serviceProvider = ServiceProviderHelper
            .GetServiceProvider(services => services
                .AddFakeLogger()
                .AddSilverback()
                .UseInMemoryLock());
        IDistributedLockFactory lockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();

        string lockName = $"shared-lock-{Guid.NewGuid():N}";
        using TestDistributedBackgroundService service1 = new(
            async stoppingToken => await ExecuteTask(stoppingToken, () => executed1 = true),
            lockFactory.GetDistributedLock(new InMemoryLockSettings(lockName), serviceProvider));
        using TestDistributedBackgroundService service2 = new(
            async stoppingToken => await ExecuteTask(stoppingToken, () => executed2 = true),
            lockFactory.GetDistributedLock(new InMemoryLockSettings(lockName), serviceProvider));

        service2.DistributedLock.ShouldBeSameAs(service1.DistributedLock);

        async Task ExecuteTask(CancellationToken stoppingToken, Action execute)
        {
            Interlocked.Increment(ref executingCount);

            try
            {
                execute.Invoke();

                if (Volatile.Read(ref executingCount) > 1)
                    executedInParallel = true;

                await Task.Delay(100, stoppingToken);
            }
            finally
            {
                Interlocked.Decrement(ref executingCount);
            }
        }

        await service1.StartAsync(CancellationToken.None);
        await service2.StartAsync(CancellationToken.None);

        await AsyncTestingUtil.WaitAsync(() => executed1 && executed2, TimeSpan.FromSeconds(5));

        executed1.ShouldBeTrue();
        executed2.ShouldBeTrue();
        executedInParallel.ShouldBeFalse();
    }

    [Fact]
    public async Task StopAsync_ShouldNotLogException_WhenOperationIsCanceledWhileAcquiringLock()
    {
        LoggerSubstitute<DistributedBackgroundService> logger = new(LogLevel.Trace);
        CancelableDistributedLock distributedLock = new();

        using TestDistributedBackgroundService service = new(_ => Task.CompletedTask, distributedLock, CreateSilverbackLogger(logger));
        await service.StartAsync(CancellationToken.None);
        await distributedLock.AcquiringTask;

        await service.StopAsync(CancellationToken.None);

        logger.DidNotReceive(LogLevel.Error, typeof(OperationCanceledException), eventId: 42);
    }

    [Fact]
    public async Task StartAsync_ShouldLogException_WhenOperationIsCanceledWithoutStopping()
    {
        LoggerSubstitute<DistributedBackgroundService> logger = new(LogLevel.Trace);
        IDistributedLock distributedLock = Substitute.For<IDistributedLock>();
        distributedLock.AcquireAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

        using TestDistributedBackgroundService service = new(_ => Task.CompletedTask, distributedLock, CreateSilverbackLogger(logger));
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        logger.Received(LogLevel.Error, typeof(OperationCanceledException), eventId: 42);
    }

    private static SilverbackLogger<DistributedBackgroundService> CreateSilverbackLogger(LoggerSubstitute<DistributedBackgroundService> logger) =>
        new(new MappedLevelsLogger<DistributedBackgroundService>([], logger));

    private sealed class TestDistributedBackgroundService : DistributedBackgroundService
    {
        private readonly Func<CancellationToken, Task> _task;

        public TestDistributedBackgroundService(
            Func<CancellationToken, Task> task,
            IDistributedLock distributedLock,
            ISilverbackLogger<DistributedBackgroundService>? logger = null)
            : base(distributedLock, logger ?? Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
            DistributedLock = distributedLock;
            _task = task;
        }

        public new IDistributedLock DistributedLock { get; }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => _task.Invoke(stoppingToken);
    }

    // Throws a plain OperationCanceledException on cancellation, like the PostgreSQL advisory lock does
    private sealed class CancelableDistributedLock : IDistributedLock
    {
        private readonly TaskCompletionSource _acquiringTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task AcquiringTask => _acquiringTaskCompletionSource.Task;

        public async ValueTask<DistributedLockHandle> AcquireAsync(CancellationToken cancellationToken = default)
        {
            _acquiringTaskCompletionSource.TrySetResult();

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Rethrown as OperationCanceledException
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }
}
