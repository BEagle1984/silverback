// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public class SilverbackBuilderWithLogLevelsExtensionsTests
{
    [Fact]
    public void WithLogLevels_WithLogLevels_LogLevelMappingCorrectlyBuilt()
    {
        ServiceCollection services = new();

        services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithLogLevels(
                configurator => configurator
                    .SetLogLevel(CoreLogEvents.DistributedLockAcquired.EventId, LogLevel.Information)
                    .SetLogLevel(CoreLogEvents.FailedToAcquireDistributedLock.EventId, LogLevel.Warning));

        ServiceProvider? serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<ISilverbackLogger<object>>().Should().NotBeNull();
    }
}
