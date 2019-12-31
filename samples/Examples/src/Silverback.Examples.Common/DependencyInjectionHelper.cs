// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Silverback.Examples.Common.Data;

namespace Silverback.Examples.Common
{
    public static class DependencyInjectionHelper
    {
        public static IServiceCollection GetServiceCollection(string sqlServerConnectionString) =>
            new ServiceCollection()
                .AddDbContext<ExamplesDbContext>(options => options
                    .UseSqlServer(sqlServerConnectionString))
                .AddLogging(l => l
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddSerilog());
    }
}