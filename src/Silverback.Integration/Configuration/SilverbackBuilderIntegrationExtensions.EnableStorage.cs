// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the EnableStorage method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Registers the types needed to use some storage (e.g. a database for the outbox).
    /// </summary>
    /// <remarks>
    ///     This method is called implicitly by <see cref="WithConnectionToMessageBroker" />.
    /// </remarks>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder EnableStorage(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        if (!builder.Services.ContainsAny<SilverbackStorageInitializer>())
        {
            builder.Services.AddSingleton<SilverbackStorageInitializer>();
        }

        return builder;
    }
}
