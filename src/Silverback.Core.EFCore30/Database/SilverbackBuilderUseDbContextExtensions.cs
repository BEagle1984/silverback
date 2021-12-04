// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Configuration;
using Silverback.Database;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Contains the <see cref="UseDbContext{TDbContext}" /> extension to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderUseDbContextExtensions
{
    /// <summary>
    ///     Registers the specified <see cref="DbContext" /> to be used as underlying storage for the services requiring it.
    /// </summary>
    /// <typeparam name="TDbContext">
    ///     The type of the <see cref="DbContext" /> to be used.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> to add the <see cref="DbContext" /> to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder UseDbContext<TDbContext>(this SilverbackBuilder builder)
        where TDbContext : DbContext
    {
        Check.NotNull(builder, nameof(builder));

        SilverbackQueryableExtensions.Implementation = new EfCoreQueryableExtensions();

        builder.Services.AddScoped<IDbContext>(serviceProvider => new EfCoreDbContext<TDbContext>(serviceProvider.GetRequiredService<TDbContext>()));

        return builder;
    }
}
