// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddSingletonOutboundRouter </c> method to the <see cref="IServiceCollection" />.
    /// </summary>
    public static class ServiceCollectionAddOutboundRouterExtensions
    {
        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <paramref name="routerType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="routerType">
        ///     The type of the outbound router to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonOutboundRouter(this IServiceCollection services, Type routerType)
        {
            if (routerType == null)
                throw new ArgumentNullException(nameof(routerType));

            services.AddSingleton(routerType);

            return services;
        }

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <typeparamref name="TRouter" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TRouter"> The type of the outbound router to add. </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonOutboundRouter<TRouter>(this IServiceCollection services)
            where TRouter : class, IOutboundRouter =>
            AddSingletonOutboundRouter(services, typeof(TRouter));

        /// <summary>
        ///     Adds a singleton outbound router with a factory specified in
        ///     <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory"> The factory that creates the service. </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonOutboundRouter(
            this IServiceCollection services,
            Func<IServiceProvider, IOutboundRouter> implementationFactory)
        {
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            services.AddSingleton(implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a singleton outbound router with an instance specified in
        ///     <paramref name="implementationInstance" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationInstance"> The instance of the service. </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonOutboundRouter(
            this IServiceCollection services,
            IOutboundRouter implementationInstance)
        {
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            services.AddSingleton(implementationInstance);

            return services;
        }
    }
}
