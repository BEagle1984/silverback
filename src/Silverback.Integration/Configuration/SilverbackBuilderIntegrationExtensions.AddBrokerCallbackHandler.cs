// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddBrokerClientCallback methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
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
    public static SilverbackBuilder AddTransientBrokerClientCallback(this SilverbackBuilder builder, Type handlerType)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(handlerType, nameof(handlerType));

        builder.Services.AddTransient(typeof(IBrokerClientCallback), handlerType);
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
    public static SilverbackBuilder AddTransientBrokerClientCallback<THandler>(this SilverbackBuilder builder)
        where THandler : class, IBrokerClientCallback =>
        AddTransientBrokerClientCallback(builder, typeof(THandler));

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
    public static SilverbackBuilder AddTransientBrokerClientCallback(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerClientCallback> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationFactory, nameof(implementationFactory));

        builder.Services.AddTransient(typeof(IBrokerClientCallback), implementationFactory);
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
    public static SilverbackBuilder AddScopedBrokerClientCallback(this SilverbackBuilder builder, Type handlerType)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(handlerType, nameof(handlerType));

        builder.Services.AddScoped(typeof(IBrokerClientCallback), handlerType);
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
    public static SilverbackBuilder AddScopedBrokerClientCallback<THandler>(this SilverbackBuilder builder)
        where THandler : class, IBrokerClientCallback =>
        AddScopedBrokerClientCallback(builder, typeof(THandler));

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
    public static SilverbackBuilder AddScopedBrokerClientCallback(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerClientCallback> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationFactory, nameof(implementationFactory));

        builder.Services.AddScoped(typeof(IBrokerClientCallback), implementationFactory);
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
    public static SilverbackBuilder AddSingletonBrokerClientCallback(this SilverbackBuilder builder, Type handlerType)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(handlerType, nameof(handlerType));

        builder.Services.AddSingleton(typeof(IBrokerClientCallback), handlerType);
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
    public static SilverbackBuilder AddSingletonBrokerClientCallback<THandler>(this SilverbackBuilder builder)
        where THandler : class, IBrokerClientCallback =>
        AddSingletonBrokerClientCallback(builder, typeof(THandler));

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
    public static SilverbackBuilder AddSingletonBrokerClientCallback(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerClientCallback> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationFactory, nameof(implementationFactory));

        builder.Services.AddSingleton(typeof(IBrokerClientCallback), implementationFactory);
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
    public static SilverbackBuilder AddSingletonBrokerClientCallback(
        this SilverbackBuilder builder,
        IBrokerClientCallback implementationInstance)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(implementationInstance, nameof(implementationInstance));

        builder.Services.AddSingleton(typeof(IBrokerClientCallback), implementationInstance);
        return builder;
    }
}
