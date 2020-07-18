// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>WithLogLevels</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderWithLogLevelsExtensions
    {
        /// <summary>
        ///     Configures the log levels that should be used to log the standard Silverback events.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" />.
        /// </param>
        /// <param name="logLevelsConfigurationAction">
        ///     The log levels configuration action.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder WithLogLevels(
            this ISilverbackBuilder silverbackBuilder,
            Action<ILogLevelConfigurator> logLevelsConfigurationAction)
        {
            Check.NotNull(logLevelsConfigurationAction, nameof(logLevelsConfigurationAction));

            var configurator = new LogLevelConfigurator();
            logLevelsConfigurationAction(configurator);

            silverbackBuilder.Services.Replace(ServiceDescriptor.Singleton(configurator.Build()));

            return silverbackBuilder;
        }
    }
}
