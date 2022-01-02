// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the AddEndpoints method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Adds the broker endpoints.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="EndpointsConfigurationBuilder" /> and configures the outbound and
    ///     inbound endpoints.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddEndpoints(this SilverbackBuilder builder, Action<EndpointsConfigurationBuilder> configureAction)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(configureAction, nameof(configureAction));

        builder.AddEndpointsConfigurator(_ => new GenericEndpointsConfigurator(configureAction));

        return builder;
    }
}
