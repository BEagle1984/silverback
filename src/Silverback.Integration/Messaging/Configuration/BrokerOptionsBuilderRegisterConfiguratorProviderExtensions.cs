// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> RegisterConfigurator </c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderRegisterConfiguratorProviderExtensions
    {
        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <typeparam name="TConfigurator">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder RegisterConfigurator<TConfigurator>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TConfigurator : class, IEndpointsConfigurator
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddTransient<IEndpointsConfigurator, TConfigurator>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="configuratorType">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder RegisterConfigurator(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            Type configuratorType)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddTransient(
                typeof(IEndpointsConfigurator),
                configuratorType);

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Registers an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder RegisterConfigurator(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddTransient(implementationFactory);

            return brokerOptionsBuilder;
        }
    }
}
