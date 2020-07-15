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
            var logLevelMapping = new LogLevelMapping();
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelMapping);

            logger.Log(LogLevel.Information, EventIds.KafkaConsumerConsumingMessage, "Log Message");

            internalLogger.Received(LogLevel.Information, null, "Log Message");
        }

        [Fact]
        public void Log_ConfiguredLogLevelMapping_LogLevelChanged()
        {
            var logLevelMapping = new LogLevelMapping
            {
                { EventIds.KafkaConsumerConsumingMessage, LogLevel.Error }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelMapping);

            logger.Log(LogLevel.Information, EventIds.KafkaConsumerConsumingMessage, "Log Message");

            internalLogger.Received(LogLevel.Error, null, "Log Message");
        }

        [Fact]
        public void Log_EventIdNotConfigured_LogLevelNotChanged()
        {
            var logLevelMapping = new LogLevelMapping
            {
                { EventIds.KafkaConsumerConsumingMessage, LogLevel.Error }
            };
            var internalLogger = new LoggerSubstitute<object>();
            var logger = new SilverbackLogger<object>(internalLogger, logLevelMapping);

            logger.Log(LogLevel.Information, EventIds.KafkaConsumerConsumingCanceled, "Log Message");

            internalLogger.Received(LogLevel.Information, null, "Log Message");
        }
    }
}
