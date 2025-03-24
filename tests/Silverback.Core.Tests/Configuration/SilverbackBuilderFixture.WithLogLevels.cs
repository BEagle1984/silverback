// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Configuration;

public partial class SilverbackBuilderFixture
{
    [Fact]
    public void WithLogLevels_ShouldSetLogLevelsDictionary()
    {
        ServiceCollection services = [];

        services
            .AddFakeLogger()
            .AddSilverback()
            .WithLogLevels(
                configurator => configurator
                    .SetLogLevel(CoreLogEvents.BackgroundServiceException.EventId, LogLevel.Information)
                    .SetLogLevel(CoreLogEvents.LockAcquired.EventId, LogLevel.Warning));

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<ISilverbackLogger<object>>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<LogLevelDictionary>().Count.ShouldBe(2);
    }
}
