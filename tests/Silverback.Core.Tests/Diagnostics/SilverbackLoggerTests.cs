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

            logger.Log(LogLevel.Information, EventIds.KafkaConsumerConsumingMessage, "Log Message");

            internalLogger.Received(LogLevel.Information, null, "Log Message");
        }

        [Fact]
        public void Log_ConfiguredLogLevelMapping_LogLevelChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                { EventIds.KafkaConsumerConsumingMessage, (e, l) => LogLevel.Error }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(LogLevel.Information, EventIds.KafkaConsumerConsumingMessage, "Log Message");

            internalLogger.Received(LogLevel.Error, null, "Log Message");
        }

        [Fact]
        public void Log_EventIdNotConfigured_LogLevelNotChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                { EventIds.KafkaConsumerConsumingMessage, (e, l) => LogLevel.Error }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelDictionary);

            logger.Log(LogLevel.Information, EventIds.KafkaConsumerConsumingCanceled, "Log Message");

            internalLogger.Received(LogLevel.Information, null, "Log Message");
        }

        [Fact]
        public void Log_ConfiguredConditionalLogLevelMapping_LogLevelChanged()
        {
            var logLevelDictionary = new LogLevelDictionary
            {
                {
                    EventIds.KafkaConsumerConsumingMessage,
                    (exception, originalLogLevel) =>
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
                EventIds.KafkaConsumerConsumingMessage,
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
                    EventIds.KafkaConsumerConsumingMessage,
                    (exception, originalLogLevel) =>
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
                EventIds.KafkaConsumerConsumingMessage,
                new ArgumentException("param"),
                "Log Message");

            internalLogger.Received(LogLevel.Information, typeof(ArgumentException), "Log Message");
        }
    }
}
