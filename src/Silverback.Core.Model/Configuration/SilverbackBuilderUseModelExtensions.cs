// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="UseModel" /> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderUseModelExtensions
{
    /// <summary>
    ///     Registers the more specific publishers <see cref="IEventPublisher" />, <see cref="ICommandPublisher" /> and
    ///     <see cref="IQueryPublisher" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder UseModel(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services
            .AddScoped<ICommandPublisher, CommandPublisher>()
            .AddScoped<IQueryPublisher, QueryPublisher>()
            .AddScoped<IEventPublisher, EventPublisher>();

        return builder;
    }
}
