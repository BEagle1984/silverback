// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddEndpointsConfigurator methods to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Adds an <see cref="IBrokerClientsConfigurator" /> to be used to setup the broker endpoints.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <typeparam name="TConfigurator">
    ///     The type of the <see cref="IBrokerClientsConfigurator" /> to add.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddBrokerClientsConfigurator<TConfigurator>(this SilverbackBuilder builder)
        where TConfigurator : class, IBrokerClientsConfigurator
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddScoped<IBrokerClientsConfigurator, TConfigurator>();

        return builder;
    }

    /// <summary>
    ///     Adds an <see cref="IBrokerClientsConfigurator" /> to be used to setup the broker endpoints.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="configuratorType">
    ///     The type of the <see cref="IBrokerClientsConfigurator" /> to add.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddBrokerClientsConfigurator(this SilverbackBuilder builder, Type configuratorType)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddScoped(typeof(IBrokerClientsConfigurator), configuratorType);

        return builder;
    }

    /// <summary>
    ///     Adds an <see cref="IBrokerClientsConfigurator" /> to be used to setup the broker endpoints.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="implementationFactory">
    ///     The factory that creates the <see cref="IBrokerClientsConfigurator" /> to add.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: review summary and document rename in release notes
    public static SilverbackBuilder AddBrokerClientsConfigurator(
        this SilverbackBuilder builder,
        Func<IServiceProvider, IBrokerClientsConfigurator> implementationFactory)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Services.AddScoped(implementationFactory);

        return builder;
    }
}
