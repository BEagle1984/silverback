// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddTransientBehavior </c>, <c> AddScopedBehavior </c> and
    ///     <c> AddSingletonBehavior </c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class ServiceCollectionAddBehaviorExtensions
    {
        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="IServiceCollection" /> to add the behavior to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientBehavior(this IServiceCollection services, Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            services.AddTransient(typeof(IBehavior), behaviorType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior"> The type of the behavior to add. </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBehavior =>
            AddTransientBehavior(services, typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory"> The factory that creates the service. </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services.AddTransient(typeof(IBehavior), implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
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
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedBehavior(this IServiceCollection services, Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            services.AddScoped(typeof(IBehavior), behaviorType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <typeparam name="TBehavior"> The type of the behavior to add. </typeparam>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBehavior =>
            AddScopedBehavior(services, typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory"> The factory that creates the service. </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services.AddScoped(typeof(IBehavior), implementationFactory);

            return services;
        }

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
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBehavior(this IServiceCollection services, Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            services.AddSingleton(typeof(IBehavior), behaviorType);

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
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBehavior =>
            AddSingletonBehavior(services, typeof(TBehavior));

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
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services.AddSingleton(typeof(IBehavior), implementationFactory);

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
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonBehavior(
            this IServiceCollection services,
            IBehavior implementationInstance)
        {
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            services.AddSingleton(typeof(IBehavior), implementationInstance);

            return services;
        }
    }
}
