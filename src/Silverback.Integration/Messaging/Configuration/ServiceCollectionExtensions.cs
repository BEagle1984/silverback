// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region RegisterConfigurator

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <typeparam name="TConfigurator">The type of the <see cref="IEndpointsConfigurator" /> to add.</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator<TConfigurator>(this IServiceCollection services)
            where TConfigurator : class, IEndpointsConfigurator =>
            services.AddTransient<IEndpointsConfigurator, TConfigurator>();

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="configuratorType">The type of the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator(
            this IServiceCollection services,
            Type configuratorType) =>
            services.AddTransient(typeof(IEndpointsConfigurator), configuratorType);

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        public static IServiceCollection AddEndpointsConfigurator(
            this IServiceCollection services,
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory) =>
            services.AddTransient(implementationFactory);

        #endregion

        #region AddSingletonBrokerBehavior

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonBrokerBehavior(this IServiceCollection services, Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            services.AddSingleton(typeof(IBrokerBehavior), behaviorType);

            return services;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonBrokerBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBrokerBehavior =>
            AddSingletonBrokerBehavior(services, typeof(TBehavior));

        /// <summary>
        ///     Adds a singleton behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonBrokerBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBrokerBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services.AddSingleton(typeof(IBrokerBehavior), implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a singleton behavior with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <seealso cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />
        public static IServiceCollection AddSingletonBrokerBehavior(
            this IServiceCollection services,
            IBrokerBehavior implementationInstance)
        {
            if (implementationInstance == null) throw new ArgumentNullException(nameof(implementationInstance));

            services.AddSingleton(typeof(IBrokerBehavior), implementationInstance);

            return services;
        }

        #endregion

        #region AddSingletonOutboundRouter

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <paramref name="routerType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="routerType">The type of the outbound router to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonOutboundRouter(this IServiceCollection services, Type routerType)
        {
            if (routerType == null) throw new ArgumentNullException(nameof(routerType));

            services.AddSingleton(routerType);

            return services;
        }

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <typeparamref name="TRouter" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TRouter">The type of the outbound router to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonOutboundRouter<TRouter>(this IServiceCollection services)
            where TRouter : class, IOutboundRouter =>
            AddSingletonOutboundRouter(services, typeof(TRouter));

        /// <summary>
        ///     Adds a singleton outbound router with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonOutboundRouter(
            this IServiceCollection services,
            Func<IServiceProvider, IOutboundRouter> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services.AddSingleton(implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a singleton outbound router with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <seealso cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />
        public static IServiceCollection AddSingletonOutboundRouter(
            this IServiceCollection services,
            IOutboundRouter implementationInstance)
        {
            if (implementationInstance == null) throw new ArgumentNullException(nameof(implementationInstance));

            services.AddSingleton(implementationInstance);

            return services;
        }

        #endregion
    }
}