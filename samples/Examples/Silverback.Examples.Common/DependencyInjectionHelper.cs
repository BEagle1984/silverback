using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Silverback.Examples.Common
{
    public static class DependencyInjectionHelper
    {
        public static IServiceCollection GetServiceCollection() => new ServiceCollection()
            .AddLogging(logging => logging
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole());
    }
}
