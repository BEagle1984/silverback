// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics
{
    public class SilverbackLoggerTests
    {
        [Fact]
        public void Log_EmptyLogLevelMapping_LogLevelNotChanged()
        {
            var logLevels = new LogLevelDictionary();
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Information);
            var mappedLevelsLogger =
                new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

            silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

            logger.Received(LogLevel.Information, null, "Acquired lock name (id).");
        }

        [Fact]
        public void Log_ConfiguredLogLevelMapping_LogLevelChanged()
        {
            var logLevels = new LogLevelDictionary
            {
                { CoreLogEvents.DistributedLockAcquired.EventId, (_, _, _) => LogLevel.Error }
            };
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Information);
            var mappedLevelsLogger =
                new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

            silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

            logger.Received(LogLevel.Error, null, "Acquired lock name (id).");
        }

        [Fact]
        public void Log_EventIdNotConfigured_LogLevelNotChanged()
        {
            var logLevels = new LogLevelDictionary
            {
                { CoreLogEvents.BackgroundServiceStarting.EventId, (_, _, _) => LogLevel.Error }
            };
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Information);
            var mappedLevelsLogger = new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

            silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

            logger.Received(LogLevel.Information, null, "Acquired lock name (id).");
        }

        [Fact]
        public void Log_ConfiguredConditionalLogLevelMapping_LogLevelChanged()
        {
            var logLevels = new LogLevelDictionary
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
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Error);
            var mappedLevelsLogger = new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

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
            var logLevels = new LogLevelDictionary
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
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Debug);
            var mappedLevelsLogger = new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

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
            var logLevels = new LogLevelDictionary
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
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Information);
            var mappedLevelsLogger = new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

            silverbackLogger.LogLockAcquired(new DistributedLockSettings("name", "id"));

            logger.Received(LogLevel.Error, null, "Acquired lock name (id).");
        }

        [Fact]
        public void IsEnabled_ConfiguredLogLevelMapping_CheckedAccordingToNewLevel()
        {
            var logger = new LoggerSubstitute<SilverbackLoggerTests>(LogLevel.Information);
            var logEvent = CoreLogEvents.AcquiringDistributedLock;
            logger.IsEnabled(logEvent.Level).Should().BeTrue();

            var logLevels = new LogLevelDictionary
            {
                { logEvent.EventId, (_, _, _) => LogLevel.Trace }
            };
            var mappedLevelsLogger = new MappedLevelsLogger<SilverbackLoggerTests>(logger, logLevels);
            var silverbackLogger = new SilverbackLogger<SilverbackLoggerTests>(mappedLevelsLogger);

            var result = silverbackLogger.IsEnabled(logEvent);

            result.Should().BeFalse();
        }
    }
}
