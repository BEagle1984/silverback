// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Contains the <c>UseDbContext</c> extension for the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        ///     Registers the specified <see cref="DbContext" /> to be used as underlying storage for the services
        ///     requiring it.
        /// </summary>
        /// <typeparam name="TDbContext">
        ///     The type of the <see cref="DbContext" /> to be used.
        /// </typeparam>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder" /> to add the <see cref="DbContext" /> to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder UseDbContext<TDbContext>(this ISilverbackBuilder builder)
            where TDbContext : DbContext
        {
            Check.NotNull(builder, nameof(builder));

            SilverbackQueryableExtensions.Implementation = new EfCoreQueryableExtensions();

            builder.Services.AddScoped<IDbContext>(
                serviceProvider => new EfCoreDbContext<TDbContext>(serviceProvider.GetRequiredService<TDbContext>()));

            return builder;
        }
    }
}
