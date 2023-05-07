// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the <see cref="AddInMemoryLock" /> and <see cref="UseInMemoryLock" /> methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderMemoryExtensions
{
    /// <summary>
    ///     Replaces all distributed locks with an in-memory version that is suitable for testing purposes only.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder UseInMemoryLock(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.AddInMemoryLock();

        DistributedLockFactory lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>() ??
                                             throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        lockFactory.OverrideFactories(_ => new InMemoryLock());

        return builder;
    }

    /// <summary>
    ///     Adds the in-memory lock.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddInMemoryLock(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        DistributedLockFactory lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>() ??
                                             throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        if (!lockFactory.HasFactory<InMemoryLockSettings>())
            lockFactory.AddFactory<InMemoryLockSettings>(_ => new InMemoryLock());

        return builder;
    }
}
