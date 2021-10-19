// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddMockedMqtt</c> method to the <see cref="BrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderAddMockedMqttExtensions
{
    /// <summary>
    ///     Registers Apache Mqtt as message broker but replaces the MQTT connectivity based on MQTTnet
    ///     with a mocked in-memory message broker that <b>more or less</b> replicates the MQTT broker behavior.
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
    ///     add the services to.
    /// </param>
    /// <param name="optionsAction">
    ///     Configures the mock options.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddMockedMqtt(
        this BrokerOptionsBuilder brokerOptionsBuilder,
        Action<IMockedMqttOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

        brokerOptionsBuilder.AddMqtt();
        brokerOptionsBuilder.SilverbackBuilder.UseMockedMqtt(optionsAction);

        return brokerOptionsBuilder;
    }
}
