// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class SilverbackBuilderWithLogLevelsExtensionsTests
    {
        [Fact]
        public void WithLogLevels_WithLogLevels_LogLevelMappingCorrectlyBuild()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithLogLevels(c => c
                    .SetLogLevel(EventIds.BrokerConnected, LogLevel.Information)
                    .SetLogLevel(EventIds.BrokerConnecting, LogLevel.Warning));

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<ISilverbackLogger>().Should().NotBeNull();
            serviceProvider.GetRequiredService<ISilverbackLogger<object>>().Should().NotBeNull();
        }
    }
}
