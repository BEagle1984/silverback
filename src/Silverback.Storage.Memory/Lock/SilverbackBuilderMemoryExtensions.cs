// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     Adds the <see cref="AddInMemoryStorage" /> and <c>UseInMemory</c> methods to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderMemoryExtensions
{
    /// <summary>
    ///     Adds all types necessary for in-memory storage.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    // public static SilverbackBuilder AddInMemoryStorage(this SilverbackBuilder builder)
    // {
    //     Check.NotNull(builder, nameof(builder));
    //
    //     DistributedLockFactory? lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>();
    //
    //     if (lockFactory == null)
    //         throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");
    //
    //     lockFactory.AddFactory<InMemoryLockSettings>(settings => new InMemoryLock(settings));
    //
    //     return builder;
    // }

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

        DistributedLockFactory? lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>();

        if (lockFactory == null)
            throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        lockFactory.OverrideFactories(settings => new InMemoryLock(settings));

        return builder;
    }

    /// <summary>
    ///     Replaces all distributed locks with an in-memory version that is suitable for testing purposes only.
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

        DistributedLockFactory? lockFactory = builder.Services.GetSingletonServiceInstance<DistributedLockFactory>();

        if (lockFactory == null)
            throw new InvalidOperationException("DistributedLockFactory not found, AddSilverback has not been called.");

        if (!lockFactory.HasFactory<InMemoryLockSettings>())
            lockFactory.AddFactory<InMemoryLockSettings>(settings => new InMemoryLock(settings));

        return builder;
    }
}
