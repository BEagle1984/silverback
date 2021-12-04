// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddSubscriber methods to the <see cref="SilverbackBuilder" />.
/// </content>
public partial class SilverbackBuilder
{
    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register and the implementation to use.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber(Type subscriberType, bool autoSubscribeAllPublicMethods = true) =>
        AddTransientSubscriber(
            subscriberType,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register and the implementation to use.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber(
        Type subscriberType,
        TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(options, nameof(options));

        Services.AddTransient(subscriberType);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(subscriberType, options);

        return this;
    }

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber<TSubscriber>(bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddTransientSubscriber(typeof(TSubscriber), autoSubscribeAllPublicMethods);

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber<TSubscriber>(TypeSubscriptionOptions options)
        where TSubscriber : class =>
        AddTransientSubscriber(typeof(TSubscriber), options);

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber(
        Type subscriberType,
        Func<IServiceProvider, object> implementationFactory,
        bool autoSubscribeAllPublicMethods = true) =>
        AddTransientSubscriber(
            subscriberType,
            implementationFactory,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber(
        Type subscriberType,
        Func<IServiceProvider, object> implementationFactory,
        TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(implementationFactory, nameof(implementationFactory));
        Check.NotNull(options, nameof(options));

        Services.AddTransient(subscriberType, implementationFactory);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(subscriberType, options);

        return this;
    }

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber<TSubscriber>(
        Func<IServiceProvider, TSubscriber> implementationFactory,
        bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddTransientSubscriber(
            implementationFactory,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddTransientSubscriber<TSubscriber>(
        Func<IServiceProvider, TSubscriber> implementationFactory,
        TypeSubscriptionOptions options)
        where TSubscriber : class
    {
        Check.NotNull(implementationFactory, nameof(implementationFactory));
        Check.NotNull(options, nameof(options));

        Services.AddTransient(implementationFactory);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(typeof(TSubscriber), options);

        return this;
    }

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register and the implementation to use.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber(Type subscriberType, bool autoSubscribeAllPublicMethods = true) =>
        AddScopedSubscriber(
            subscriberType,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register and the implementation to use.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber(Type subscriberType, TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(options, nameof(options));

        Services.AddScoped(subscriberType);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(subscriberType, options);

        return this;
    }

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber<TSubscriber>(bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddScopedSubscriber(typeof(TSubscriber), autoSubscribeAllPublicMethods);

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber<TSubscriber>(TypeSubscriptionOptions options)
        where TSubscriber : class =>
        AddScopedSubscriber(typeof(TSubscriber), options);

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber(
        Type subscriberType,
        Func<IServiceProvider, object> implementationFactory,
        bool autoSubscribeAllPublicMethods = true) =>
        AddScopedSubscriber(
            subscriberType,
            implementationFactory,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber(
        Type subscriberType,
        Func<IServiceProvider, object> implementationFactory,
        TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(implementationFactory, nameof(implementationFactory));
        Check.NotNull(options, nameof(options));

        Services.AddScoped(subscriberType, implementationFactory);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(
            subscriberType,
            options);

        return this;
    }

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber<TSubscriber>(
        Func<IServiceProvider, TSubscriber> implementationFactory,
        bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddScopedSubscriber(
            implementationFactory,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a factory
    ///     specified in <paramref name="implementationFactory" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddScopedSubscriber<TSubscriber>(
        Func<IServiceProvider, TSubscriber> implementationFactory,
        TypeSubscriptionOptions options)
        where TSubscriber : class
    {
        Check.NotNull(implementationFactory, nameof(implementationFactory));
        Check.NotNull(options, nameof(options));

        Services.AddScoped(implementationFactory);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(typeof(TSubscriber), options);

        return this;
    }

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register and the implementation to use.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber(
        Type subscriberType,
        bool autoSubscribeAllPublicMethods = true) =>
        AddSingletonSubscriber(
            subscriberType,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register and the implementation to use.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber(
        Type subscriberType,
        TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(options, nameof(options));

        Services.AddSingleton(subscriberType);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(subscriberType, options);

        return this;
    }

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to this
    ///     <see cref="SilverbackBuilder" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber<TSubscriber>(bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddSingletonSubscriber(typeof(TSubscriber), autoSubscribeAllPublicMethods);

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to this
    ///     <see cref="SilverbackBuilder" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber<TSubscriber>(TypeSubscriptionOptions options)
        where TSubscriber : class =>
        AddSingletonSubscriber(typeof(TSubscriber), options);

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
    ///     factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber(
        Type subscriberType,
        Func<IServiceProvider, object> implementationFactory,
        bool autoSubscribeAllPublicMethods = true) =>
        AddSingletonSubscriber(
            subscriberType,
            implementationFactory,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
    ///     factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber(
        Type subscriberType,
        Func<IServiceProvider, object> implementationFactory,
        TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(implementationFactory, nameof(implementationFactory));
        Check.NotNull(options, nameof(options));

        Services.AddSingleton(subscriberType, implementationFactory);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(subscriberType, options);

        return this;
    }

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
    ///     factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber<TSubscriber>(
        Func<IServiceProvider, TSubscriber> implementationFactory,
        bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddSingletonSubscriber(
            implementationFactory,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
    ///     factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to add.
    /// </typeparam>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber<TSubscriber>(
        Func<IServiceProvider, TSubscriber> implementationFactory,
        TypeSubscriptionOptions options)
        where TSubscriber : class
    {
        Check.NotNull(implementationFactory, nameof(implementationFactory));
        Check.NotNull(options, nameof(options));

        Services.AddSingleton(implementationFactory);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(typeof(TSubscriber), options);

        return this;
    }

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
    ///     instance specified in <paramref name="implementationInstance" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
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
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber(
        Type subscriberType,
        object implementationInstance,
        bool autoSubscribeAllPublicMethods = true) =>
        AddSingletonSubscriber(
            subscriberType,
            implementationInstance,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
    ///     instance specified in <paramref name="implementationInstance" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="subscriberType">
    ///     The type of the subscriber to register.
    /// </param>
    /// <param name="implementationInstance">
    ///     The instance of the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber(
        Type subscriberType,
        object implementationInstance,
        TypeSubscriptionOptions options)
    {
        Check.NotNull(subscriberType, nameof(subscriberType));
        Check.NotNull(implementationInstance, nameof(implementationInstance));
        Check.NotNull(options, nameof(options));

        Services.AddSingleton(subscriberType, implementationInstance);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(subscriberType, options);

        return this;
    }

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
    ///     instance specified in <paramref name="implementationInstance" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to register.
    /// </typeparam>
    /// <param name="implementationInstance">
    ///     The instance of the service.
    /// </param>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber<TSubscriber>(
        TSubscriber implementationInstance,
        bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddSingletonSubscriber(
            implementationInstance,
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an instance specified in
    ///     <paramref name="implementationInstance" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The type of the subscriber to register.
    /// </typeparam>
    /// <param name="implementationInstance">
    ///     The instance of the service.
    /// </param>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public SilverbackBuilder AddSingletonSubscriber<TSubscriber>(
        TSubscriber implementationInstance,
        TypeSubscriptionOptions options)
        where TSubscriber : class
    {
        Check.NotNull(implementationInstance, nameof(implementationInstance));
        Check.NotNull(options, nameof(options));

        Services.AddSingleton(implementationInstance);
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(typeof(TSubscriber), options);

        return this;
    }

    /// <summary>
    ///     Registers the base type to be resolved as subscriber. The actual types have to be added to the
    ///     <see cref="IServiceCollection" /> separately.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The base type of the subscribers (class or interface).
    /// </typeparam>
    /// <param name="autoSubscribeAllPublicMethods">
    ///     A boolean value indicating whether all public methods of the specified type have to be automatically
    ///     subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    /// <remarks>
    ///     The subscribers will have to be registered twice (with the base type and the type itself:
    ///     <c>.AddScoped&lt;BaseType, Subscriber&gt;.AddScoped&lt;Subscriber&gt;</c>).
    /// </remarks>
    public SilverbackBuilder AddSubscribers<TSubscriber>(bool autoSubscribeAllPublicMethods = true)
        where TSubscriber : class =>
        AddSubscribers<TSubscriber>(
            new TypeSubscriptionOptions
            {
                AutoSubscribeAllPublicMethods = autoSubscribeAllPublicMethods
            });

    /// <summary>
    ///     Registers the base type to be resolved as subscriber. The actual types have to be added to the
    ///     <see cref="IServiceCollection" /> separately.
    /// </summary>
    /// <typeparam name="TSubscriber">
    ///     The base type of the subscribers (class or interface).
    /// </typeparam>
    /// <param name="options">
    ///     The <see cref="TypeSubscriptionOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    /// <remarks>
    ///     The subscribers will have to be registered twice (with the base type and the type itself:
    ///     <c>.AddScoped&lt;BaseType, Subscriber&gt;.AddScoped&lt;Subscriber&gt;</c>).
    /// </remarks>
    public SilverbackBuilder AddSubscribers<TSubscriber>(TypeSubscriptionOptions options)
        where TSubscriber : class
    {
        Check.NotNull(options, nameof(options));
        BusOptions.Subscriptions.AddTypeSubscriptionIfNotExists(typeof(TSubscriber), options);

        return this;
    }
}
