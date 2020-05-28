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
    public static class SilverbackBuilderAddSubscriberExtensions
    {
        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber(baseType, subscriberType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber(subscriberType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber<TBase, TSubscriber>();
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber<TSubscriber>(this ISilverbackBuilder silverbackBuilder)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber<TSubscriber>();
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
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
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber(baseType, subscriberType, implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber(subscriberType, implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber<TBase, TSubscriber>(implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddTransientSubscriber(implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber(baseType, subscriberType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber(subscriberType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber<TBase, TSubscriber>();
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber<TSubscriber>(this ISilverbackBuilder silverbackBuilder)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber<TSubscriber>();
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
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
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber(
                baseType,
                subscriberType,
                implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber(subscriberType, implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber<TBase, TSubscriber>(implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
        ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddScopedSubscriber(implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="baseType">
        ///     The subscribers base class or interface.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(baseType, subscriberType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(subscriberType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to this
        ///     <see cref="ISilverbackBuilder" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber<TBase, TSubscriber>();
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to this
        ///     <see cref="ISilverbackBuilder" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(this ISilverbackBuilder silverbackBuilder)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber<TSubscriber>();
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
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
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(baseType, subscriberType, implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(subscriberType, implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber<TBase, TSubscriber>(implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
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
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type baseType,
            Type subscriberType,
            ISubscriber implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(baseType, subscriberType, implementationInstance);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="subscriberType">
        ///     The type of the subscriber to register.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            ISubscriber implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(subscriberType, implementationInstance);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">
        ///     The subscribers base class or interface.
        /// </typeparam>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to register.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber<TBase, TSubscriber>(implementationInstance);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The type of the subscriber to register.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonSubscriber(implementationInstance);
            return silverbackBuilder;
        }
    }
}
