// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddEndpointsConfigurator </c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddEndpointsConfiguratorExtensions
    {
        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <typeparam name="TConfigurator">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddEndpointsConfigurator<TConfigurator>(
            this ISilverbackBuilder silverbackBuilder)
            where TConfigurator : class, IEndpointsConfigurator
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddEndpointsConfigurator<TConfigurator>();

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="configuratorType">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddEndpointsConfigurator(
            this ISilverbackBuilder silverbackBuilder,
            Type configuratorType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddEndpointsConfigurator(configuratorType);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddEndpointsConfigurator(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddEndpointsConfigurator(implementationFactory);

            return silverbackBuilder;
        }
    }
}
