// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Xunit;

namespace Silverback.Tests.Core.Diagnostics
{
    public class SilverbackLoggerTests
    {
        [Fact]
        public void Log_EmptyLogLevelMapping_LogLevelNotChanged()
        {
            var logLevelDictionary = new LogLevelDictionary();
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(LogLevel.Information, CoreEventIds.BackgroundServiceStarting, "Log Message");

            internalLogger.Received(LogLevel.Information, null, "Log Message");
        }

        [Fact]
        public void Log_ConfiguredLogLevelMapping_LogLevelChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                { CoreEventIds.BackgroundServiceStarting, (e, l, m) => LogLevel.Error }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(LogLevel.Information, CoreEventIds.BackgroundServiceStarting, "Log Message");

            internalLogger.Received(LogLevel.Error, null, "Log Message");
        }

        [Fact]
        public void Log_EventIdNotConfigured_LogLevelNotChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                { CoreEventIds.BackgroundServiceStarting, (e, l, m) => LogLevel.Error }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(LogLevel.Information, CoreEventIds.BackgroundServiceLockAcquired, "Log Message");

            internalLogger.Received(LogLevel.Information, null, "Log Message");
        }

        [Fact]
        public void Log_ConfiguredConditionalLogLevelMapping_LogLevelChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                {
                    CoreEventIds.BackgroundServiceStarting,
                    (exception, originalLogLevel, message) =>
                    {
                        if (exception is InvalidOperationException)
                        {
                            return LogLevel.Error;
                        }

                        return originalLogLevel;
                    }
                }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(
                LogLevel.Information,
                CoreEventIds.BackgroundServiceStarting,
                new InvalidOperationException(),
                "Log Message");

            internalLogger.Received(LogLevel.Error, typeof(InvalidOperationException), "Log Message");
        }

        [Fact]
        public void Log_ConfiguredConditionalLogLevelMapping_LogLevelNotChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                {
                    CoreEventIds.BackgroundServiceStarting,
                    (exception, originalLogLevel, message) =>
                    {
                        if (exception is InvalidOperationException)
                        {
                            return LogLevel.Error;
                        }

                        return originalLogLevel;
                    }
                }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(
                LogLevel.Information,
                CoreEventIds.BackgroundServiceStarting,
                new ArgumentException("param"),
                "Log Message");

            internalLogger.Received(LogLevel.Information, typeof(ArgumentException), "Log Message");
        }

        [Fact]
        public void Log_ConfiguredMessageBasedLogLevelMapping_LogLevelChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                {
                    CoreEventIds.BackgroundServiceStarting,
                    (exception, originalLogLevel, message) =>
                    {
                        if (message.Value == "TestMessage 10")
                        {
                            return LogLevel.Error;
                        }

                        return originalLogLevel;
                    }
                }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(
                LogLevel.Information,
                CoreEventIds.BackgroundServiceStarting,
                new ArgumentException("param"),
                "TestMessage {Value}",
                10);

            internalLogger.Received(LogLevel.Error, typeof(ArgumentException), "TestMessage 10");
        }
    }
}
