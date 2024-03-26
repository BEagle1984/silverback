// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the <see cref="AddPostgreSqlAdvisoryLock" /> method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderPostgreSqlExtensions
{
    /// <summary>
    ///     Adds the PostgreSql advisory locks based lock.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddPostgreSqlAdvisoryLock(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        DistributedLockFactory lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>() ??
                                             throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        if (!lockFactory.HasFactory<PostgreSqlAdvisoryLockSettings>())
        {
            lockFactory.AddFactory<PostgreSqlAdvisoryLockSettings>(
                (settings, serviceProvider) => new PostgreSqlAdvisoryLock(
                    settings,
                    serviceProvider.GetRequiredService<ISilverbackLogger<PostgreSqlAdvisoryLock>>()));
        }

        return builder;
    }

    /// <summary>
    ///     Adds the PostgreSql table based lock.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddPostgreSqlTableLock(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        DistributedLockFactory lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>() ??
                                             throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        if (!lockFactory.HasFactory<PostgreSqlTableLockSettings>())
        {
            lockFactory.AddFactory<PostgreSqlTableLockSettings>(
                (settings, serviceProvider) => new PostgreSqlTableLock(
                    settings,
                    serviceProvider.GetRequiredService<ISilverbackLogger<PostgreSqlTableLock>>()));
        }

        return builder;
    }
}
