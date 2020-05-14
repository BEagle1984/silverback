// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds the <c>AddEndpointsConfigurator</c> method to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionAddEndpointsConfiguratorExtensions
    {
        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <typeparam name="TConfigurator">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddEndpointsConfigurator<TConfigurator>(this IServiceCollection services)
            where TConfigurator : class, IEndpointsConfigurator =>
            services.AddTransient<IEndpointsConfigurator, TConfigurator>();

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="configuratorType">
        ///     The type of the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddEndpointsConfigurator(
            this IServiceCollection services,
            Type configuratorType) =>
            services.AddTransient(typeof(IEndpointsConfigurator), configuratorType);

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the <see cref="IEndpointsConfigurator" /> to add.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddEndpointsConfigurator(
            this IServiceCollection services,
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory) =>
            services.AddTransient(implementationFactory);
    }
}
