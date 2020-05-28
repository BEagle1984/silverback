// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>
    ///         UseModel
    ///     </c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        ///     Registers the default messages model from Silverback.Core.Model package and the specific publishers
        ///     (<see cref="IEventPublisher" />, <see cref="ICommandPublisher" /> and <see cref="IQueryPublisher" />
        ///     ).
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="ISilverbackBuilder" /> to add the model types to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder UseModel(this ISilverbackBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services
                .AddScoped<ICommandPublisher, CommandPublisher>()
                .AddScoped<IQueryPublisher, QueryPublisher>()
                .AddScoped<IEventPublisher, EventPublisher>();

            return builder;
        }
    }
}
