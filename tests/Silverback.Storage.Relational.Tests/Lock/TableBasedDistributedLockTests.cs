// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Relational.Lock;

public class TableBasedDistributedLockTests
{
    private readonly LoggerSubstitute<TableBasedDistributedLock> _loggerSubstitute;

    private readonly ISilverbackLogger<TableBasedDistributedLock> _silverbackLogger;

    public TableBasedDistributedLockTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback());

        _loggerSubstitute = (LoggerSubstitute<TableBasedDistributedLock>)serviceProvider
            .GetRequiredService<ILogger<TableBasedDistributedLock>>();
        _silverbackLogger = serviceProvider.GetRequiredService<ISilverbackLogger<TableBasedDistributedLock>>();
    }

    [Fact]
    public async Task AcquireAsync_ShouldThrowWithoutTryingToAcquire_WhenCancellationTokenIsAlreadyCanceled()
    {
        TestLock distributedLock = new(new TestLockSettings(), _silverbackLogger);
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        Task<DistributedLockHandle> acquireTask = distributedLock.AcquireAsync(cancellationTokenSource.Token).AsTask();

        await acquireTask.ShouldThrowAsync<OperationCanceledException>();
        distributedLock.TryAcquireCount.ShouldBe(0);
    }

    [Fact]
    public async Task DisposeAsync_ShouldLogReleaseFailure_WhenReleaseThrows()
    {
        TestLock distributedLock = new(new TestLockSettings(), _silverbackLogger)
        {
            ReleaseException = new InvalidOperationException("release failed")
        };

        DistributedLockHandle handle = await distributedLock.AcquireAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await handle.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        distributedLock.ReleasedHandlerName.ShouldBe(distributedLock.AcquiredHandlerName);
        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidOperationException),
            "Failed to release lock test-lock",
            66,
            "release failed");
    }

    private sealed record TestLockSettings : TableBasedDistributedLockSettings
    {
        public TestLockSettings()
            : base("test-lock")
        {
        }
    }

    private sealed class TestLock : TableBasedDistributedLock
    {
        public TestLock(TableBasedDistributedLockSettings settings, ISilverbackLogger<TableBasedDistributedLock> logger)
            : base(settings, logger)
        {
        }

        public Exception? ReleaseException { get; init; }

        public int TryAcquireCount { get; private set; }

        public string? AcquiredHandlerName { get; private set; }

        public string? ReleasedHandlerName { get; private set; }

        protected override Task<bool> TryAcquireLockAsync(string handlerName)
        {
            TryAcquireCount++;
            AcquiredHandlerName = handlerName;

            return Task.FromResult(true);
        }

        protected override Task UpdateHeartbeatAsync(string handlerName) => Task.CompletedTask;

        protected override Task ReleaseLockAsync(string handlerName)
        {
            ReleasedHandlerName = handlerName;

            return ReleaseException != null ? Task.FromException(ReleaseException) : Task.CompletedTask;
        }
    }
}
