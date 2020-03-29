// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Silverback.Tests
{
    public static class NullLoggerServiceCollectionExtensions
    {
        public static IServiceCollection AddNullLogger(this IServiceCollection services) => services
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }
}