// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics;

public class CoreLoggerExtensionsTests
{
    private readonly LoggerSubstitute<CoreLoggerExtensionsTests> _logger;

    private readonly SilverbackLogger<CoreLoggerExtensionsTests> _silverbackLogger;

    public CoreLoggerExtensionsTests()
    {
        LogLevelDictionary logLevels = new();
        _logger = new LoggerSubstitute<CoreLoggerExtensionsTests>(LogLevel.Trace);
        MappedLevelsLogger<CoreLoggerExtensionsTests> mappedLevelsLogger = new(logLevels, _logger);
        _silverbackLogger = new SilverbackLogger<CoreLoggerExtensionsTests>(mappedLevelsLogger);
    }

    [Fact]
    public void LogSubscriberResultDiscarded_Logged()
    {
        string expectedMessage =
            "Discarding result of type TypeName because it doesn't match the expected return type " +
            "ExpectedTypeName.";

        _silverbackLogger.LogSubscriberResultDiscarded("TypeName", "ExpectedTypeName");

        _logger.Received(LogLevel.Debug, null, expectedMessage, 11);
    }

    [Fact]
    public void LogAcquiringLock_Logged()
    {
        string expectedMessage = "Trying to acquire lock lock-name (lock-id)...";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogAcquiringLock(lockSettings);

        _logger.Received(LogLevel.Information, null, expectedMessage, 21);
    }

    [Fact]
    public void LogLockAcquired_Logged()
    {
        string expectedMessage = "Acquired lock lock-name (lock-id).";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogLockAcquired(lockSettings);

        _logger.Received(LogLevel.Information, null, expectedMessage, 22);
    }

    [Fact]
    public void LogFailedToAcquireLock_Logged()
    {
        string expectedMessage = "Failed to acquire lock lock-name (lock-id).";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogFailedToAcquireLock(lockSettings, new TimeoutException());

        _logger.Received(LogLevel.Debug, typeof(TimeoutException), expectedMessage, 23);
    }

    [Fact]
    public void LogLockReleased_Logged()
    {
        string expectedMessage = "Released lock lock-name (lock-id).";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogLockReleased(lockSettings);

        _logger.Received(LogLevel.Information, null, expectedMessage, 24);
    }

    [Fact]
    public void LogFailedToReleaseLock_Logged()
    {
        string expectedMessage = "Failed to release lock lock-name (lock-id).";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogFailedToReleaseLock(lockSettings, new TimeoutException());

        _logger.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 25);
    }

    [Fact]
    public void LogFailedToCheckLock_Logged()
    {
        string expectedMessage = "Failed to check lock lock-name (lock-id).";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogFailedToCheckLock(lockSettings, new TimeoutException());

        _logger.Received(LogLevel.Error, typeof(TimeoutException), expectedMessage, 26);
    }

    [Fact]
    public void LogFailedToSendLockHeartbeat_Logged()
    {
        string expectedMessage = "Failed to send heartbeat for lock lock-name (lock-id).";
        DistributedLockSettings lockSettings = new("lock-name", "lock-id");

        _silverbackLogger.LogFailedToSendLockHeartbeat(lockSettings, new TimeoutException());

        _logger.Received(LogLevel.Error, typeof(TimeoutException), expectedMessage, 27);
    }

    [Fact]
    public void LogBackgroundServiceStarting_Logged()
    {
        string expectedMessage =
            "Starting background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsTests+FakeBackgroundService...";

        _silverbackLogger.LogBackgroundServiceStarting(new FakeBackgroundService());

        _logger.Received(LogLevel.Information, null, expectedMessage, 41);
    }

    [Fact]
    public void LogBackgroundServiceLockAcquired_Logged()
    {
        string expectedMessage =
            "Lock acquired, executing background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsTests+FakeBackgroundService.";

        _silverbackLogger.LogBackgroundServiceLockAcquired(new FakeBackgroundService());

        _logger.Received(LogLevel.Information, null, expectedMessage, 42);
    }

    [Fact]
    public void LogBackgroundServiceException_Logged()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsTests+FakeBackgroundService " +
            "execution failed.";

        _silverbackLogger.LogBackgroundServiceException(
            new FakeBackgroundService(),
            new TimeoutException());

        _logger.Received(LogLevel.Error, typeof(TimeoutException), expectedMessage, 43);
    }

    [Fact]
    public void LogRecurringBackgroundServiceStopped_Logged()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsTests+FakeBackgroundService " +
            "stopped.";

        _silverbackLogger.LogRecurringBackgroundServiceStopped(new FakeBackgroundService());

        _logger.Received(LogLevel.Information, null, expectedMessage, 51);
    }

    [Fact]
    public void LogRecurringBackgroundServiceSleeping_Logged()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsTests+FakeBackgroundService " +
            "sleeping for 10000 milliseconds.";

        _silverbackLogger.LogRecurringBackgroundServiceSleeping(
            new FakeBackgroundService(),
            TimeSpan.FromSeconds(10));

        _logger.Received(LogLevel.Debug, null, expectedMessage, 52);
    }

    [Fact]
    public void LogRecurringBackgroundServiceException_Logged()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsTests+FakeBackgroundService " +
            "execution failed.";

        _silverbackLogger.LogRecurringBackgroundServiceException(
            new FakeBackgroundService(),
            new TimeoutException());

        _logger.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 53);
    }

    private sealed class FakeBackgroundService : DistributedBackgroundService
    {
        public FakeBackgroundService()
            : base(
                Substitute.For<IDistributedLockManager>(),
                Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
        }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) =>
            throw new NotSupportedException();
    }
}
