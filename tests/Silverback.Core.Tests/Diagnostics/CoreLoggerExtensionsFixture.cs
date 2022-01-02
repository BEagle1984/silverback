// Copyright (c) 2020 Sergio Aquilini
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
        LogLevelDictionary logLevels = new();
        _logger = new LoggerSubstitute<CoreLoggerExtensionsFixture>(LogLevel.Trace);
        MappedLevelsLogger<CoreLoggerExtensionsFixture> mappedLevelsLogger = new(logLevels, _logger);
        _silverbackLogger = new SilverbackLogger<CoreLoggerExtensionsFixture>(mappedLevelsLogger);
    }

    [Fact]
    public void LogSubscriberResultDiscarded_ShouldLog()
    {
        string expectedMessage =
            "Discarding result of type TypeName because it doesn't match the expected return type " +
            "ExpectedTypeName.";

        _silverbackLogger.LogSubscriberResultDiscarded("TypeName", "ExpectedTypeName");

        _logger.Received(LogLevel.Debug, null, expectedMessage, 11);
    }

    [Fact]
    public void LogBackgroundServiceStarting_ShouldLog()
    {
        string expectedMessage =
            "Starting background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService...";

        _silverbackLogger.LogBackgroundServiceStarting(new FakeBackgroundService());

        _logger.Received(LogLevel.Information, null, expectedMessage, 41);
    }

    [Fact]
    public void LogBackgroundServiceLockAcquired_ShouldLog()
    {
        string expectedMessage =
            "Lock acquired, executing background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService.";

        _silverbackLogger.LogBackgroundServiceLockAcquired(new FakeBackgroundService());

        _logger.Received(LogLevel.Information, null, expectedMessage, 42);
    }

    [Fact]
    public void LogBackgroundServiceException_ShouldLog()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService " +
            "execution failed.";

        _silverbackLogger.LogBackgroundServiceException(
            new FakeBackgroundService(),
            new TimeoutException());

        _logger.Received(LogLevel.Error, typeof(TimeoutException), expectedMessage, 43);
    }

    [Fact]
    public void LogRecurringBackgroundServiceStopped_ShouldLog()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService " +
            "stopped.";

        _silverbackLogger.LogRecurringBackgroundServiceStopped(new FakeBackgroundService());

        _logger.Received(LogLevel.Information, null, expectedMessage, 51);
    }

    [Fact]
    public void LogRecurringBackgroundServiceSleeping_ShouldLog()
    {
        string expectedMessage =
            "Background service " +
            "Silverback.Tests.Core.Diagnostics.CoreLoggerExtensionsFixture+FakeBackgroundService " +
            "sleeping for 10000 milliseconds.";

        _silverbackLogger.LogRecurringBackgroundServiceSleeping(
            new FakeBackgroundService(),
            TimeSpan.FromSeconds(10));

        _logger.Received(LogLevel.Debug, null, expectedMessage, 52);
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
