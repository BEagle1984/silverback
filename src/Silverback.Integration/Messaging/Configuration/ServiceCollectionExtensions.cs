// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region RegisterConfigurator

        /// <summary>
        /// Adds an <see cref="IEndpointsConfigurator"/> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <typeparam name="TConfigurator">The type of the <see cref="IEndpointsConfigurator"/> to add.</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator<TConfigurator>(this IServiceCollection services) 
            where TConfigurator : class, IEndpointsConfigurator =>
            services.AddTransient<IEndpointsConfigurator, TConfigurator>();

        /// <summary>
        /// Adds an <see cref="IEndpointsConfigurator"/> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <param name="configuratorType">The type of the <see cref="IEndpointsConfigurator"/> to add.</param>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator(
            this IServiceCollection services, Type configuratorType) =>
            services.AddTransient(typeof(IEndpointsConfigurator), configuratorType);
        
        /// <summary>
        /// Adds an <see cref="IEndpointsConfigurator"/> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <param name="implementationFactory">The factory that creates the <see cref="IEndpointsConfigurator"/> to add.</param>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator(
            this IServiceCollection services, Func<IServiceProvider, IEndpointsConfigurator> implementationFactory) =>
            services.AddTransient(implementationFactory);
        
        #endregion
    }
}