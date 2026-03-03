// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="AddApplicationPublisher" /> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderAddApplicationPublisherExtensions
{
    /// <summary>
    ///     Registers the enhanced <see cref="IApplicationPublisher" /> adding specific methods to publish
    /// <see cref="ICommand"/>/<see cref="ICommand{TResult}"/>, <see cref="IQuery{TResult}"/>, and <see cref="IEvent"/> messages.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddApplicationPublisher(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services
            .AddTransient<IApplicationPublisher, ApplicationPublisher>();

        return builder;
    }
}
