// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Extension to configure the loglevels of Silverback.
    /// </summary>
    public static class SilverbackBuilderWithLogLevelsExtensions
    {
        /// <summary>
        /// Configure the loglevels that should be used to log events.
        /// </summary>
        /// <param name="silverbackBuilder">An <see cref="ISilverbackBuilder"/>.</param>
        /// <param name="configure">The action that configures the loglevels.</param>
        /// <returns>The <see cref="ISilverbackBuilder"/>.</returns>
        public static ISilverbackBuilder WithLogLevels(this ISilverbackBuilder silverbackBuilder, Action<ILogLevelConfigurator> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var configurator = new LogLevelConfigurator();
            configure(configurator);

            silverbackBuilder.Services.Replace(ServiceDescriptor.Singleton(configurator.Build()));

            return silverbackBuilder;
        }
    }
}
