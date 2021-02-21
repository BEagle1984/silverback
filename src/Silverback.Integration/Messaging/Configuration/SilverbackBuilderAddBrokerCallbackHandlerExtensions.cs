// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddBrokerCallbackHandler</c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddBrokerCallbackHandlerExtensions
    {
        /// <summary>
        ///     Adds a transient callback of the type specified in <paramref name="handlerType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="handlerType">
        ///     The type of the handler to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            Type handlerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(handlerType, nameof(handlerType));

            silverbackBuilder.Services.AddTransient(typeof(IBrokerCallback), handlerType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a transient callback of the type specified in <typeparamref name="THandler" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="THandler">
        ///     The type of the handler to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBrokerCallbackHandler<THandler>(
            this ISilverbackBuilder silverbackBuilder)
            where THandler : class, IBrokerCallback =>
            AddTransientBrokerCallbackHandler(silverbackBuilder, typeof(THandler));

        /// <summary>
        ///     Adds a transient callback with a factory specified in <paramref name="implementationFactory" /> to
        ///     the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBrokerCallback> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddTransient(typeof(IBrokerCallback), implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped callback of the type specified in <paramref name="handlerType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="handlerType">
        ///     The type of the handler to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            Type handlerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(handlerType, nameof(handlerType));

            silverbackBuilder.Services.AddScoped(typeof(IBrokerCallback), handlerType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped callback of the type specified in <typeparamref name="THandler" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="THandler">
        ///     The type of the handler to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBrokerCallbackHandler<THandler>(
            this ISilverbackBuilder silverbackBuilder)
            where THandler : class, IBrokerCallback =>
            AddScopedBrokerCallbackHandler(silverbackBuilder, typeof(THandler));

        /// <summary>
        ///     Adds a scoped callback with a factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBrokerCallback> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddScoped(typeof(IBrokerCallback), implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton callback of the type specified in <paramref name="handlerType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="handlerType">
        ///     The type of the handler to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            Type handlerType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(handlerType, nameof(handlerType));

            silverbackBuilder.Services.AddSingleton(typeof(IBrokerCallback), handlerType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton callback of the type specified in <typeparamref name="THandler" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="THandler">
        ///     The type of the handler to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerCallbackHandler<THandler>(
            this ISilverbackBuilder silverbackBuilder)
            where THandler : class, IBrokerCallback =>
            AddSingletonBrokerCallbackHandler(silverbackBuilder, typeof(THandler));

        /// <summary>
        ///     Adds a singleton callback with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBrokerCallback> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddSingleton(typeof(IBrokerCallback), implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton callback with an instance specified in <paramref name="implementationInstance" />
        ///     to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the handler to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerCallbackHandler(
            this ISilverbackBuilder silverbackBuilder,
            IBrokerCallback implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            silverbackBuilder.Services.AddSingleton(typeof(IBrokerCallback), implementationInstance);
            return silverbackBuilder;
        }
    }
}
