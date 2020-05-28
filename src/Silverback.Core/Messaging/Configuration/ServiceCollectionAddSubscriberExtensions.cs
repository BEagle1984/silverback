// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>
    ///         AddTransientSubscriber
    ///     </c>, <c>
    ///         AddScopedSubscriber
    ///     </c> and <c>
    ///         AddSingletonSubscriber
    ///     </c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class ServiceCollectionAddSubscriberExtensions
    {
        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="IServiceCollection" /> to add the subscriber to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType)
        {
            Check.NotNull(baseType, nameof(baseType));
            Check.NotNull(subscriberType, nameof(subscriberType));

            services
                .AddTransient(subscriberType)
                .AddTransient(baseType, subscriberType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection
            AddTransientSubscriber(this IServiceCollection services, Type subscriberType) =>
            AddTransientSubscriber(services, typeof(ISubscriber), subscriberType);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber<TBase, TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber(services, typeof(TBase), typeof(TSubscriber));

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber<TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber<ISubscriber, TSubscriber>(services);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            Check.NotNull(baseType, nameof(baseType));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services
                .AddTransient(subscriberType, implementationFactory)
                .AddTransient(baseType, implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddTransientSubscriber(services, typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddTransientSubscriber<TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber<ISubscriber, TSubscriber>(services, implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType)
        {
            Check.NotNull(baseType, nameof(baseType));
            Check.NotNull(subscriberType, nameof(subscriberType));

            services
                .AddScoped(subscriberType)
                .AddScoped(baseType, subscriberType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber(this IServiceCollection services, Type subscriberType) =>
            AddScopedSubscriber(services, typeof(ISubscriber), subscriberType);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber<TBase, TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber(services, typeof(TBase), typeof(TSubscriber));

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber<TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber<ISubscriber, TSubscriber>(services);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            Check.NotNull(baseType, nameof(baseType));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services
                .AddScoped(subscriberType, implementationFactory)
                .AddScoped(baseType, implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddScopedSubscriber(services, typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddScopedSubscriber<TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber<ISubscriber, TSubscriber>(services, implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType)
        {
            Check.NotNull(baseType, nameof(baseType));
            Check.NotNull(subscriberType, nameof(subscriberType));

            services
                .AddSingleton(subscriberType)
                .AddSingleton(baseType, serviceProvider => serviceProvider.GetRequiredService(subscriberType));

            return services;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection
            AddSingletonSubscriber(this IServiceCollection services, Type subscriberType) =>
            AddSingletonSubscriber(services, typeof(ISubscriber), subscriberType);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber<TBase, TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(TBase), typeof(TSubscriber));

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber<TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber<ISubscriber, TSubscriber>(services);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            Check.NotNull(baseType, nameof(baseType));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            services
                .AddSingleton(subscriberType, implementationFactory)
                .AddSingleton(baseType, serviceProvider => serviceProvider.GetRequiredService(subscriberType));

            return services;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddSingletonSubscriber(services, typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber<TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber<ISubscriber, TSubscriber>(services, implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            ISubscriber implementationInstance)
        {
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            services
                .AddSingleton(subscriberType, implementationInstance)
                .AddSingleton(baseType, serviceProvider => serviceProvider.GetRequiredService(subscriberType));

            return services;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            ISubscriber implementationInstance) =>
            AddSingletonSubscriber(services, typeof(ISubscriber), subscriberType, implementationInstance);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to register.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationInstance);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the specified
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to register.
        /// </typeparam>
        /// <param name="services">
        ///     The <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service
        ///     to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddSingletonSubscriber<TSubscriber>(
            this IServiceCollection services,
            TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(ISubscriber), typeof(TSubscriber), implementationInstance);
    }
}
