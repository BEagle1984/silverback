// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddBrokerCallbackHandler methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderExtensions
{
    /// <summary>
    ///     Adds a transient callback of the type specified in <paramref name="handlerType" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="handlerType">
    ///     The type of the handler to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientBrokerCallbackHandler(this SilverbackBuilder builder, Type handlerType)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(handlerType, nameof(handlerType));

        builder.Services.AddTransient(typeof(IBrokerCallback), handlerType);
        return builder;
    }

    /// <summary>
    ///     Adds a transient callback of the type specified in <typeparamref name="THandler" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="THandler">
    ///     The type of the handler to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientBrokerCallbackHandler<THandler>(this SilverbackBuilder builder)
        where THandler : class, IBrokerCallback =>
        AddTransientBrokerCallbackHandler(builder, typeof(THandler));

    /// <summary>
    ///     Adds a transient callback with a factory specified in <paramref name="implementationFactory" /> to
    ///     the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientBrokerCallbackHandler(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerCallback> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationFactory, nameof(implementationFactory));

        builder.Services.AddTransient(typeof(IBrokerCallback), implementationFactory);
        return builder;
    }

    /// <summary>
    ///     Adds a scoped callback of the type specified in <paramref name="handlerType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="handlerType">
    ///     The type of the handler to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddScopedBrokerCallbackHandler(this SilverbackBuilder builder, Type handlerType)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(handlerType, nameof(handlerType));

        builder.Services.AddScoped(typeof(IBrokerCallback), handlerType);
        return builder;
    }

    /// <summary>
    ///     Adds a scoped callback of the type specified in <typeparamref name="THandler" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="THandler">
    ///     The type of the handler to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddScopedBrokerCallbackHandler<THandler>(this SilverbackBuilder builder)
        where THandler : class, IBrokerCallback =>
        AddScopedBrokerCallbackHandler(builder, typeof(THandler));

    /// <summary>
    ///     Adds a scoped callback with a factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddScopedBrokerCallbackHandler(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerCallback> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationFactory, nameof(implementationFactory));

        builder.Services.AddScoped(typeof(IBrokerCallback), implementationFactory);
        return builder;
    }

    /// <summary>
    ///     Adds a singleton callback of the type specified in <paramref name="handlerType" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="handlerType">
    ///     The type of the handler to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerCallbackHandler(this SilverbackBuilder builder, Type handlerType)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(handlerType, nameof(handlerType));

        builder.Services.AddSingleton(typeof(IBrokerCallback), handlerType);
        return builder;
    }

    /// <summary>
    ///     Adds a singleton callback of the type specified in <typeparamref name="THandler" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="THandler">
    ///     The type of the handler to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerCallbackHandler<THandler>(this SilverbackBuilder builder)
        where THandler : class, IBrokerCallback =>
        AddSingletonBrokerCallbackHandler(builder, typeof(THandler));

    /// <summary>
    ///     Adds a singleton callback with a factory specified in <paramref name="implementationFactory" /> to
    ///     the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerCallbackHandler(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerCallback> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationFactory, nameof(implementationFactory));

        builder.Services.AddSingleton(typeof(IBrokerCallback), implementationFactory);
        return builder;
    }

    /// <summary>
    ///     Adds a singleton callback with an instance specified in <paramref name="implementationInstance" />
    ///     to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the handler to.
    /// </param>
    /// <param name="implementationInstance">
    ///     The instance of the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerCallbackHandler(
        this SilverbackBuilder builder,
        IBrokerCallback implementationInstance)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationInstance, nameof(implementationInstance));

        builder.Services.AddSingleton(typeof(IBrokerCallback), implementationInstance);
        return builder;
    }
}
