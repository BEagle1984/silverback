// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class SilverbackLoggerTests
    {
        [Fact]
        public void SilverbackLogger_ConfiguredLogLevel_LogLevelChanged()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton(typeof(ILogger<>), typeof(LoggerSubstitute<>))
                .AddSilverback()
                .WithLogLevels(c => c
                    .SetLogLevel(EventIds.BrokerConnected, LogLevel.Information)
                    .SetLogLevel(EventIds.BrokerConnecting, (e, l) => LogLevel.Warning));

            var serviceProvider = services.BuildServiceProvider();

            var internalLogger = (LoggerSubstitute<object>)serviceProvider.GetRequiredService<ILogger<object>>();
            var logger = serviceProvider.GetRequiredService<ISilverbackLogger<object>>();

            logger.Log(LogLevel.Error, EventIds.BrokerConnected, "Log Message 1");
            logger.Log(LogLevel.Error, EventIds.BrokerConnecting, "Log Message 2");

            internalLogger.Received(LogLevel.Information, null, "Log Message 1");
            internalLogger.Received(LogLevel.Warning, null, "Log Message 2");
        }
    }
}
