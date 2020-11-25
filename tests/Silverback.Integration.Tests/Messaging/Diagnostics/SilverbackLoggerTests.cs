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
        private readonly LoggerSubstitute<SilverbackLoggerTests> _logger = new();

        [Fact]
        public void Log_WithDefaultLogLevel_MessageLogged()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton(typeof(ILogger<SilverbackLoggerTests>), _logger)
                .AddSilverback();

            var serviceProvider = services.BuildServiceProvider();

            var silverbackLogger = serviceProvider.GetRequiredService<ISilverbackLogger<SilverbackLoggerTests>>();

            silverbackLogger.Log(LogLevel.Information, CoreEventIds.DistributedLockAcquired, "Log Message 1");

            _logger.Received(LogLevel.Information, null, "Log Message 1");
        }

        [Fact]
        public void Log_WithCustomLogLevel_LogLevelApplied()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton(typeof(ILogger<SilverbackLoggerTests>), _logger)
                .AddSilverback()
                .WithLogLevels(
                    configurator => configurator
                        .SetLogLevel(CoreEventIds.DistributedLockAcquired, LogLevel.Trace));

            var serviceProvider = services.BuildServiceProvider();

            var silverbackLogger = serviceProvider.GetRequiredService<ISilverbackLogger<SilverbackLoggerTests>>();

            silverbackLogger.Log(LogLevel.Information, CoreEventIds.DistributedLockAcquired, "Log Message 1");

            _logger.Received(LogLevel.Trace, null, "Log Message 1");
        }

        [Fact]
        public void Log_WithCustomLogLevelFunction_LogLevelApplied()
        {
            var services = new ServiceCollection();

            services
                .AddSingleton(typeof(ILogger<SilverbackLoggerTests>), _logger)
                .AddSilverback()
                .WithLogLevels(
                    configurator => configurator
                        .SetLogLevel(CoreEventIds.DistributedLockAcquired, (_, _) => LogLevel.Warning));

            var serviceProvider = services.BuildServiceProvider();

            var silverbackLogger = serviceProvider.GetRequiredService<ISilverbackLogger<SilverbackLoggerTests>>();

            silverbackLogger.Log(LogLevel.Information, CoreEventIds.DistributedLockAcquired, "Log Message 1");

            _logger.Received(LogLevel.Warning, null, "Log Message 1");
        }
    }
}
