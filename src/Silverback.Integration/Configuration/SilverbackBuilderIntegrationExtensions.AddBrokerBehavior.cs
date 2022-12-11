// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddBrokerBehavior methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Adds a transient behavior of the type specified in <paramref name="behaviorType" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <param name="behaviorType">
    ///     The type of the behavior to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientBrokerBehavior(this SilverbackBuilder builder, Type behaviorType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddTransient(typeof(IBrokerBehavior), behaviorType);

        return builder;
    }

    /// <summary>
    ///     Adds a transient behavior of the type specified in <typeparamref name="TBehavior" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TBehavior">
    ///     The type of the behavior to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientBrokerBehavior<TBehavior>(this SilverbackBuilder builder)
        where TBehavior : class, IBrokerBehavior =>
        AddTransientBrokerBehavior(builder, typeof(TBehavior));

    /// <summary>
    ///     Adds a transient behavior with a factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddTransientBrokerBehavior(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerBehavior> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddTransient(typeof(IBrokerBehavior), implementationFactory);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <param name="behaviorType">
    ///     The type of the behavior to register and the implementation to use.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerBehavior(this SilverbackBuilder builder, Type behaviorType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(IBrokerBehavior), behaviorType);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <typeparam name="TBehavior">
    ///     The type of the behavior to add.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerBehavior<TBehavior>(this SilverbackBuilder builder)
        where TBehavior : class, IBrokerBehavior =>
        AddSingletonBrokerBehavior(builder, typeof(TBehavior));

    /// <summary>
    ///     Adds a singleton behavior with a factory specified in <paramref name="implementationFactory" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerBehavior(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerBehavior> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(IBrokerBehavior), implementationFactory);

        return builder;
    }

    /// <summary>
    ///     Adds a singleton behavior with an instance specified in <paramref name="implementationInstance" /> to the
    ///     <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the behavior to.
    /// </param>
    /// <param name="implementationInstance">
    ///     The instance of the service.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddSingletonBrokerBehavior(this SilverbackBuilder builder, IBrokerBehavior implementationInstance)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddSingleton(typeof(IBrokerBehavior), implementationInstance);

        return builder;
    }
}
