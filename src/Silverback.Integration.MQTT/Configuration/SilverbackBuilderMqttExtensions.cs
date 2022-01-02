// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <c>AddMqttEndpoints</c> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderMqttExtensions
{
    /// <summary>
    ///     Adds the MQTT endpoints.
    /// </summary>
    /// <param name="silverbackBuilder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttEndpointsConfigurationBuilder" /> and adds
    ///     the outbound and inbound endpoints.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddMqttEndpoints(
        this SilverbackBuilder silverbackBuilder,
        Action<MqttEndpointsConfigurationBuilder> configureAction)
    {
        Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
        Check.NotNull(configureAction, nameof(configureAction));

        return silverbackBuilder.AddEndpointsConfigurator(_ => new MqttEndpointsConfigurator(configureAction));
    }
}
