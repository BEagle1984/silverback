// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics;

public class SilverbackLoggerTests
{
    [Fact]
    public void Log_EmptyLogLevelMapping_LogLevelNotChanged()
    {
        LogLevelDictionary logLevels = new();
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

        logger.Received(LogLevel.Information, null, "Acquired lock name (id).");
    }

    [Fact]
    public void Log_ConfiguredLogLevelMapping_LogLevelChanged()
    {
        LogLevelDictionary logLevels = new()
        {
            { CoreLogEvents.DistributedLockAcquired.EventId, (_, _, _) => LogLevel.Error }
        };
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

        logger.Received(LogLevel.Error, null, "Acquired lock name (id).");
    }

    [Fact]
    public void Log_EventIdNotConfigured_LogLevelNotChanged()
    {
        LogLevelDictionary logLevels = new()
        {
            { CoreLogEvents.BackgroundServiceStarting.EventId, (_, _, _) => LogLevel.Error }
        };
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

        logger.Received(LogLevel.Information, null, "Acquired lock name (id).");
    }

    [Fact]
    public void Log_ConfiguredConditionalLogLevelMapping_LogLevelChanged()
    {
        LogLevelDictionary logLevels = new()
        {
            {
                CoreLogEvents.FailedToAcquireDistributedLock.EventId,
                (exception, originalLogLevel, _) =>
                {
                    if (exception is InvalidOperationException)
                        return LogLevel.Error;

                    return originalLogLevel;
                }
            }
        };
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Error);
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogFailedToAcquireLock(
            new DistributedLockSettings("name", "id"),
            new InvalidOperationException());

        logger.Received(
            LogLevel.Error,
            typeof(InvalidOperationException),
            "Failed to acquire lock name (id).");
    }

    [Fact]
    public void Log_ConfiguredConditionalLogLevelMapping_LogLevelNotChanged()
    {
        LogLevelDictionary logLevels = new()
        {
            {
                CoreLogEvents.FailedToAcquireDistributedLock.EventId,
                (exception, originalLogLevel, _) =>
                {
                    if (exception is InvalidCastException)
                        return LogLevel.Error;

                    return originalLogLevel;
                }
            }
        };
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Debug);
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogFailedToAcquireLock(
            new DistributedLockSettings("name", "id"),
            new InvalidOperationException());

        logger.Received(
            LogLevel.Debug,
            typeof(InvalidOperationException),
            "Failed to acquire lock name (id).");
    }

    [Fact]
    public void Log_ConfiguredMessageBasedLogLevelMapping_LogLevelChanged()
    {
        LogLevelDictionary logLevels = new()
        {
            {
                CoreLogEvents.DistributedLockAcquired.EventId,
                (_, originalLogLevel, message) =>
                {
                    if (message.Value == "Acquired lock name (id).")
                        return LogLevel.Error;

                    return originalLogLevel;
                }
            }
        };
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Information);
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

        logger.Received(LogLevel.Error, null, "Acquired lock name (id).");
    }

    [Fact]
    public void IsEnabled_ConfiguredLogLevelMapping_CheckedAccordingToNewLevel()
    {
        LoggerSubstitute<SilverbackLoggerTests> logger = new(LogLevel.Information);
        LogEvent logEvent = CoreLogEvents.AcquiringDistributedLock;
        logger.IsEnabled(logEvent.Level).Should().BeTrue();

        LogLevelDictionary logLevels = new()
        {
            { logEvent.EventId, (_, _, _) => LogLevel.Trace }
        };
        MappedLevelsLogger<SilverbackLoggerTests> mappedLevelsLogger = new(logLevels, logger);
        SilverbackLogger<SilverbackLoggerTests> silverbackLogger = new(mappedLevelsLogger);

        bool result = silverbackLogger.IsEnabled(logEvent);

        result.Should().BeFalse();
    }
}
