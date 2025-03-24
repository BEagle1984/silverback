// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the <see cref="AddEntityFrameworkLock" /> method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderEntityFrameworkExtensions
{
    /// <summary>
    ///     Adds the Entity Framework based lock.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddEntityFrameworkLock(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        DistributedLockFactory lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>() ??
                                             throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        if (!lockFactory.HasFactory<EntityFrameworkLockSettings>())
        {
            lockFactory.AddFactory<EntityFrameworkLockSettings>(
                (settings, serviceProvider) => new EntityFrameworkLock(
                    settings,
                    serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                    serviceProvider.GetRequiredService<ISilverbackLogger<EntityFrameworkLock>>()));
        }

        return builder;
    }
}
