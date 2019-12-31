// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        ///     Registers the specified DbContext to be used as underlying storage for the
        ///     services requiring it.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the <see cref="DbContext" /> to be used.</typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ISilverbackBuilder UseDbContext<TDbContext>(this ISilverbackBuilder builder)
            where TDbContext : DbContext
        {
            SilverbackQueryableExtensions.Implementation = new EfCoreQueryableExtensions();

            builder.Services
                .AddScoped<IDbContext>(s => new EfCoreDbContext<TDbContext>(s.GetRequiredService<TDbContext>()));

            return builder;
        }
    }
}