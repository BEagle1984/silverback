// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Data;

namespace Silverback.Examples.Common
{
    public static class DependencyInjectionHelper
    {
        public static IServiceCollection GetServiceCollection() => new ServiceCollection()
            .AddDbContext<ExamplesDbContext>(options => options
                .UseSqlServer(Configuration.ConnectionString))
            .AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace))
            .AddSingleton<JobScheduler>();
    }
}
