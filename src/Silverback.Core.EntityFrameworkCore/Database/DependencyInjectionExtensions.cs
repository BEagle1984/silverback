// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDbContextAbstraction<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext =>
            services
                .AddScoped<IDbContext>(s => new EfCoreDbContext<TDbContext>(s.GetRequiredService<TDbContext>()));
    }
}