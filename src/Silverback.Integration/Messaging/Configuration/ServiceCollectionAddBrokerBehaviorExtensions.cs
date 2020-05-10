// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds the <c>AddSingletonBrokerBehavior</c> method to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionAddBrokerBehaviorExtensions
    {
        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBrokerBehavior(this IServiceCollection services, Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            services.AddSingleton(typeof(IBrokerBehavior), behaviorType);

            return services;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior"> The type of the behavior to add. </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBrokerBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBrokerBehavior =>
            AddSingletonBrokerBehavior(services, typeof(TBehavior));

        /// <summary>
        ///     Adds a singleton behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory"> The factory that creates the service. </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBrokerBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBrokerBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services.AddSingleton(typeof(IBrokerBehavior), implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a singleton behavior with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationInstance"> The instance of the service. </param>
        /// <returns>
        ///     The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBrokerBehavior(
            this IServiceCollection services,
            IBrokerBehavior implementationInstance)
        {
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            services.AddSingleton(typeof(IBrokerBehavior), implementationInstance);

            return services;
        }
    }
}
