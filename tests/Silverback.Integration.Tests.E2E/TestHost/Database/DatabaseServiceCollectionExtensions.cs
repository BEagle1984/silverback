// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Storage;

namespace Silverback.Tests.Integration.E2E.TestHost.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection InitDatabase(this IServiceCollection services, Func<SilverbackStorageInitializer, Task> initFunction) =>
        services
            .AddHostedService(
                serviceProvider => new InitDatabaseHostedService(
                    serviceProvider.GetRequiredService<SilverbackStorageInitializer>(),
                    initFunction));

    public static IServiceCollection InitDbContext<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext =>
        services
            .AddHostedService(
                serviceProvider =>
                    new InitDbContextHostedService<TDbContext>(serviceProvider.GetRequiredService<IServiceScopeFactory>()));
}
