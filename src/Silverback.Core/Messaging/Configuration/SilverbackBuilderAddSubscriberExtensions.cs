// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddTransientSubscriber</c>, <c>AddScopedSubscriber</c> and <c>AddSingletonSubscriber</c>
    ///     methods to the <see cref="ISilverbackBuilder" />.
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
        /// <param name="subscriberType">
        ///     The type of the subscriber to register and the implementation to use.
        /// </param>
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));

            silverbackBuilder.Services.AddTransient(subscriberType);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class =>
            AddTransientSubscriber(silverbackBuilder, typeof(TSubscriber), autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddTransient(subscriberType, implementationFactory);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddTransient(implementationFactory);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                typeof(TSubscriber),
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));

            silverbackBuilder.Services.AddScoped(subscriberType);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class =>
            AddScopedSubscriber(silverbackBuilder, typeof(TSubscriber), autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddScoped(subscriberType, implementationFactory);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddScoped(implementationFactory);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                typeof(TSubscriber),
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));

            silverbackBuilder.Services.AddSingleton(subscriberType);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class =>
            AddSingletonSubscriber(silverbackBuilder, typeof(TSubscriber), autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddSingleton(subscriberType, implementationFactory);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, TSubscriber> implementationFactory,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddSingleton(implementationFactory);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                typeof(TSubscriber),
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber(
            this ISilverbackBuilder silverbackBuilder,
            Type subscriberType,
            object implementationInstance,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(subscriberType, nameof(subscriberType));
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            silverbackBuilder.Services.AddSingleton(subscriberType, implementationInstance);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                subscriberType,
                autoSubscribeAllPublicMethods);

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
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            TSubscriber implementationInstance,
            bool autoSubscribeAllPublicMethods = true)
            where TSubscriber : class
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            silverbackBuilder.Services.AddSingleton(implementationInstance);
            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                typeof(TSubscriber),
                autoSubscribeAllPublicMethods);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Registers the base type to be resolved as subscriber. The actual types have to be added to the <see cref="IServiceCollection" /> separately.
        /// </summary>
        /// <typeparam name="TSubscriber">
        ///     The base type of the subscribers (class or interface).
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the subscriber to.
        /// </param>
        /// <param name="autoSubscribeAllPublicMethods">
        ///     A boolean value indicating whether all public methods of the specified type have to be automatically
        ///     subscribed. When set to <c>false</c> only the methods decorated with the
        ///     <see cref="SubscribeAttribute" /> are subscribed.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        /// <remarks>
        ///     The subscribers will have to be registered twice (with the base type and the type itself: <c>.AddScoped&lt;BaseType, Subscriber&gt;.AddScoped&lt;Subscriber&gt;</c>).
        /// </remarks>
        public static ISilverbackBuilder AddSubscribers<TSubscriber>(
            this ISilverbackBuilder silverbackBuilder,
            bool autoSubscribeAllPublicMethods = true)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.BusOptions.Subscriptions.AddTypedSubscriptionIfNotExists(
                typeof(TSubscriber),
                autoSubscribeAllPublicMethods);

            return silverbackBuilder;
        }
    }
}
