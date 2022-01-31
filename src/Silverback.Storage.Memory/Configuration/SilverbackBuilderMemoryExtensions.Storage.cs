// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the <see cref="AddInMemoryStorage" /> method to the <see cref="SilverbackBuilder" />.
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
    public static SilverbackBuilder AddInMemoryStorage(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        if (!builder.Services.ContainsAny<InMemoryStorageFactory>())
        {
            builder.Services.AddSingleton(new InMemoryStorageFactory());
        }

        return builder;
    }
}
