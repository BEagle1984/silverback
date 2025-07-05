// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics;

public class CoreLoggerExtensionsFixture
{
    private readonly LoggerSubstitute<CoreLoggerExtensionsFixture> _logger;

    private readonly SilverbackLogger<CoreLoggerExtensionsFixture> _silverbackLogger;

    public CoreLoggerExtensionsFixture()
    {
        LogLevelDictionary logLevels = [];
        _logger = new LoggerSubstitute<CoreLoggerExtensionsFixture>(LogLevel.Trace);
        MappedLevelsLogger<CoreLoggerExtensionsFixture> mappedLevelsLogger = new(logLevels, _logger);
        _silverbackLogger = new SilverbackLogger<CoreLoggerExtensionsFixture>(mappedLevelsLogger);
    }

    [Fact]
    public void LogSubscriberResultDiscarded_ShouldLog()
    {
        _silverbackLogger.LogSubscriberResultDiscarded("TypeName", "ExpectedTypeName");

        _logger.Received(
            LogLevel.Debug,
            null,
            "Discarding result of type TypeName because doesn't match expected return type ExpectedTypeName",
            11);
    }

    [Fact]
    public void LogBackgroundServiceStarting_ShouldLog()
    {
        _silverbackLogger.LogBackgroundServiceStarting(new FakeBackgroundService());

        _logger.Received(
            LogLevel.Information,
            null,
            "Starting background service Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService",
            41);
    }

    [Fact]
    public void LogBackgroundServiceException_ShouldLog()
    {
        _silverbackLogger.LogBackgroundServiceException(
            new FakeBackgroundService(),
            new TimeoutException());

        _logger.Received(
            LogLevel.Error,
            typeof(TimeoutException),
            "Background service Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService execution failed",
            42);
    }

    [Fact]
    public void LogRecurringBackgroundServiceStopped_ShouldLog()
    {
        _silverbackLogger.LogRecurringBackgroundServiceStopped(new FakeBackgroundService());

        _logger.Received(
            LogLevel.Information,
            null,
            "Background service Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService stopped",
            51);
    }

    [Fact]
    public void LogRecurringBackgroundServiceSleeping_ShouldLog()
    {
        _silverbackLogger.LogRecurringBackgroundServiceSleeping(
            new FakeBackgroundService(),
            TimeSpan.FromSeconds(10));

        _logger.Received(
            LogLevel.Debug,
            null,
            "Background service Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService sleeping for 10000 ms",
            52);
    }

    [Fact]
    public void LogLockAcquired_ShouldLog()
    {
        _silverbackLogger.LogLockAcquired("my-lock");

        _logger.Received(LogLevel.Information, null, "Lock my-lock acquired", 61);
    }

    [Fact]
    public void LogLockReleased_ShouldLog()
    {
        _silverbackLogger.LogLockReleased("my-lock");

        _logger.Received(LogLevel.Information, null, "Lock my-lock released", 62);
    }

    [Fact]
    public void LogLockLost_ShouldLog()
    {
        _silverbackLogger.LogLockLost("my-lock", new ArithmeticException());

        _logger.Received(LogLevel.Error, typeof(ArithmeticException), "Lock my-lock lost", 63);
    }

    [Fact]
    public void LogAcquireLockFailed_ShouldLog()
    {
        _silverbackLogger.LogAcquireLockFailed("my-lock", new ArithmeticException());

        _logger.Received(LogLevel.Error, typeof(ArithmeticException), "Failed to acquire lock my-lock", 64);
    }

    [Fact]
    public void LogAcquireLockConcurrencyException_ShouldLog()
    {
        _silverbackLogger.LogAcquireLockConcurrencyException("my-lock", new ArithmeticException());

        _logger.Received(LogLevel.Information, typeof(ArithmeticException), "Failed to acquire lock my-lock", 65);
    }

    [Fact]
    public void LogReleaseLockFailed_ShouldLog()
    {
        _silverbackLogger.LogReleaseLockFailed("my-lock", new ArithmeticException());

        _logger.Received(LogLevel.Error, typeof(ArithmeticException), "Failed to release lock my-lock", 66);
    }

    private sealed class FakeBackgroundService : DistributedBackgroundService
    {
        public FakeBackgroundService()
            : base(
                Substitute.For<IDistributedLock>(),
                Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
        }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) =>
            throw new NotSupportedException();
    }
}
