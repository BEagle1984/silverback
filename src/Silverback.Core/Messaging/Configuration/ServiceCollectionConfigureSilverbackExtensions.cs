// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>ConfigureSilverback</c> method to the <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionConfigureSilverbackExtensions
    {
        /// <summary>
        ///     Returns an <see cref="ISilverbackBuilder" /> instance that can be used to configure the additional
        ///     services.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="IServiceCollection" /> to add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> to add the services necessary to enable the Silverback
        ///     features.
        /// </returns>
        public static ISilverbackBuilder ConfigureSilverback(this IServiceCollection services) =>
            new SilverbackBuilder(services);
    }
}
