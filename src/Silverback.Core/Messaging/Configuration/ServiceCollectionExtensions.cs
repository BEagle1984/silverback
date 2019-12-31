// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region Subscribers

        #region AddTransientSubscriber

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));

            services
                .AddTransient(subscriberType)
                .AddTransient(baseType, subscriberType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection
            AddTransientSubscriber(this IServiceCollection services, Type subscriberType) =>
            AddTransientSubscriber(services, typeof(ISubscriber), subscriberType);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber<TBase, TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber(services, typeof(TBase), typeof(TSubscriber));

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber<TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber<ISubscriber, TSubscriber>(services);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services
                .AddTransient(subscriberType, implementationFactory)
                .AddTransient(baseType, implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddTransientSubscriber(services, typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientSubscriber<TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber<ISubscriber, TSubscriber>(services, implementationFactory);

        #endregion

        #region AddScopedSubscriber

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));

            services
                .AddScoped(subscriberType)
                .AddScoped(baseType, subscriberType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber(this IServiceCollection services, Type subscriberType) =>
            AddScopedSubscriber(services, typeof(ISubscriber), subscriberType);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber<TBase, TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber(services, typeof(TBase), typeof(TSubscriber));

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber<TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber<ISubscriber, TSubscriber>(services);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services
                .AddScoped(subscriberType, implementationFactory)
                .AddScoped(baseType, implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddScopedSubscriber(services, typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedSubscriber<TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber<ISubscriber, TSubscriber>(services, implementationFactory);

        #endregion

        #region AddSingletonSubscriber

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));

            services
                .AddSingleton(subscriberType)
                .AddSingleton(baseType, subscriberType);

            return services;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection
            AddSingletonSubscriber(this IServiceCollection services, Type subscriberType) =>
            AddSingletonSubscriber(services, typeof(ISubscriber), subscriberType);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber<TBase, TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(TBase), typeof(TSubscriber));

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber<TSubscriber>(this IServiceCollection services)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber<ISubscriber, TSubscriber>(services);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services
                .AddSingleton(subscriberType, implementationFactory)
                .AddSingleton(baseType, implementationFactory);

            return services;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddSingletonSubscriber(services, typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber<TSubscriber>(
            this IServiceCollection services,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber<ISubscriber, TSubscriber>(services, implementationFactory);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type baseType,
            Type subscriberType,
            ISubscriber implementationInstance)
        {
            if (implementationInstance == null) throw new ArgumentNullException(nameof(implementationInstance));

            services
                .AddSingleton(subscriberType, implementationInstance)
                .AddSingleton(baseType, implementationInstance);

            return services;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber(
            this IServiceCollection services,
            Type subscriberType,
            ISubscriber implementationInstance) =>
            AddSingletonSubscriber(services, typeof(ISubscriber), subscriberType, implementationInstance);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to register.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber<TBase, TSubscriber>(
            this IServiceCollection services,
            TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(TBase), typeof(TSubscriber), implementationInstance);

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddSingletonSubscriber<TSubscriber>(
            this IServiceCollection services,
            TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(services, typeof(ISubscriber), typeof(TSubscriber), implementationInstance);

        #endregion

        #endregion

        #region Behaviors

        #region AddTransientBehavior

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientBehavior(this IServiceCollection services, Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            services.AddTransient(typeof(IBehavior), behaviorType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBehavior =>
            AddTransientBehavior(services, typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddTransientBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services.AddTransient(typeof(IBehavior), implementationFactory);

            return services;
        }

        #endregion

        #region AddScopedBehavior

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedBehavior(this IServiceCollection services, Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            services.AddScoped(typeof(IBehavior), behaviorType);

            return services;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBehavior =>
            AddScopedBehavior(services, typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the
        ///     service to.
        /// </param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddScopedBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services.AddScoped(typeof(IBehavior), implementationFactory);

            return services;
        }

        #endregion

        #region AddSingletonBehavior

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
        public static IServiceCollection AddSingletonBehavior(this IServiceCollection services, Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            services.AddSingleton(typeof(IBehavior), behaviorType);

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
        public static IServiceCollection AddSingletonBehavior<TBehavior>(this IServiceCollection services)
            where TBehavior : class, IBehavior =>
            AddSingletonBehavior(services, typeof(TBehavior));

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
        public static IServiceCollection AddSingletonBehavior(
            this IServiceCollection services,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            services.AddSingleton(typeof(IBehavior), implementationFactory);

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
        public static IServiceCollection AddSingletonBehavior(
            this IServiceCollection services,
            IBehavior implementationInstance)
        {
            if (implementationInstance == null) throw new ArgumentNullException(nameof(implementationInstance));

            services.AddSingleton(typeof(IBehavior), implementationInstance);

            return services;
        }

        #endregion

        #endregion
    }
}