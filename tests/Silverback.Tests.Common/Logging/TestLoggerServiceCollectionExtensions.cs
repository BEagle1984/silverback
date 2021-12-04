// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging;

public static class TestLoggerServiceCollectionExtensions
{
    public static IServiceCollection AddFakeLogger(
        this IServiceCollection services,
        LogLevel minLevel = LogLevel.Information)
    {
        services
            .AddSingleton<ILoggerFactory>(new FakeLoggerFactory(minLevel))
            .AddSingleton(typeof(ILogger<>), typeof(FakeLogger<>));

        return services;
    }

    public static IServiceCollection AddLoggerSubstitute(
        this IServiceCollection services,
        LogLevel minLevel = LogLevel.Information)
    {
        services
            .AddSingleton<ILoggerFactory>(new LoggerSubstituteFactory(minLevel))
            .AddSingleton(typeof(ILogger<>), typeof(LoggerSubstitute<>));

        return services;
    }
}
