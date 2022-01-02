// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics;

public class SilverbackLoggerFixture
{
    [Fact]
    public void Log_ShouldUseDefaultLogLevel_WhenMappingIsEmpty()
    {
        LogLevelDictionary logLevels = new();
        LoggerSubstitute<SilverbackLoggerFixture> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerFixture> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerFixture> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogBackgroundServiceStarting(new TestService());

        logger.Received(
            LogLevel.Information,
            null,
            "Starting background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestService...");
    }

    [Fact]
    public void Log_ShouldUseMappedLogLevel()
    {
        LogLevelDictionary logLevels = new()
        {
            { CoreLogEvents.BackgroundServiceStarting.EventId, (_, _, _) => LogLevel.Error }
        };
        LoggerSubstitute<SilverbackLoggerFixture> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerFixture> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerFixture> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogBackgroundServiceStarting(new TestService());

        logger.Received(
            LogLevel.Error,
            null,
            "Starting background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestService...");
    }

    [Fact]
    public void Log_ShouldUseDefaultLogLevel_WhenEventIdIsNotMapped()
    {
        LogLevelDictionary logLevels = new()
        {
            { CoreLogEvents.BackgroundServiceLockAcquired.EventId, (_, _, _) => LogLevel.Error }
        };
        LoggerSubstitute<SilverbackLoggerFixture> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerFixture> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerFixture> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogBackgroundServiceStarting(new TestService());

        logger.Received(
            LogLevel.Information,
            null,
            "Starting background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestService...");
    }

    [Fact]
    public void Log_ShouldUseExceptionBasedConditionalLogLevelMapping()
    {
        LogLevelDictionary logLevels = new()
        {
            {
                CoreLogEvents.BackgroundServiceException.EventId,
                (exception, originalLogLevel, _) =>
                {
                    if (exception is InvalidOperationException)
                        return LogLevel.Warning;

                    return originalLogLevel;
                }
            }
        };
        LoggerSubstitute<SilverbackLoggerFixture> logger = new(LogLevel.Warning);
        MappedLevelsLogger<SilverbackLoggerFixture> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerFixture> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogBackgroundServiceException(new TestService(), new InvalidOperationException());
        silverbackLogger.LogBackgroundServiceException(new TestService(), new TimeoutException());

        logger.Received(
            LogLevel.Warning,
            typeof(InvalidOperationException),
            "Background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestService execution failed.");
        logger.Received(
            LogLevel.Error,
            typeof(TimeoutException),
            "Background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestService execution failed.");
    }

    [Fact]
    public void Log_ShouldUseMessageBasedConditionalLogLevelMapping()
    {
        LogLevelDictionary logLevels = new()
        {
            {
                CoreLogEvents.BackgroundServiceException.EventId,
                (_, originalLogLevel, message) =>
                {
                    if (message.Value.Contains("TestOtherService", StringComparison.Ordinal))
                        return LogLevel.Warning;

                    return originalLogLevel;
                }
            }
        };
        LoggerSubstitute<SilverbackLoggerFixture> logger = new(LogLevel.Warning);
        MappedLevelsLogger<SilverbackLoggerFixture> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerFixture> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogBackgroundServiceException(new TestService(), new InvalidOperationException());
        silverbackLogger.LogBackgroundServiceException(new TestOtherService(), new InvalidOperationException());

        logger.Received(
            LogLevel.Error,
            typeof(InvalidOperationException),
            "Background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestService execution failed.");
        logger.Received(
            LogLevel.Warning,
            typeof(InvalidOperationException),
            "Background service Silverback.Tests.Core.Diagnostics.SilverbackLoggerFixture+TestOtherService execution failed.");
    }

    [Fact]
    public void IsEnabled_ShouldEnableAccordingToMappedLevel()
    {
        LoggerSubstitute<SilverbackLoggerFixture> logger = new(LogLevel.Information);
        LogEvent logEvent = CoreLogEvents.BackgroundServiceStarting;
        logger.IsEnabled(logEvent.Level).Should().BeTrue();

        LogLevelDictionary logLevels = new()
        {
            { logEvent.EventId, (_, _, _) => LogLevel.Trace }
        };
        MappedLevelsLogger<SilverbackLoggerFixture> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerFixture> silverbackLogger = new(mappedLevelsLogger);

        bool result = silverbackLogger.IsEnabled(logEvent);

        result.Should().BeFalse();
    }

    private class TestService : DistributedBackgroundService
    {
        public TestService()
            : base(NullLock.Instance, Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
        }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }

    private class TestOtherService : DistributedBackgroundService
    {
        public TestOtherService()
            : base(NullLock.Instance, Substitute.For<ISilverbackLogger<DistributedBackgroundService>>())
        {
        }

        protected override Task ExecuteLockedAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
