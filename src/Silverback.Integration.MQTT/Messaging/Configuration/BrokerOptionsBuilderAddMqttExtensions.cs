// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Diagnostics.Logger;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddMqtt</c> method to the <see cref="BrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderAddMqttExtensions
{
    /// <summary>
    ///     Registers Mqtt as message broker.
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddMqtt(this BrokerOptionsBuilder brokerOptionsBuilder)
    {
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

        if (brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<MqttClientsConfigurationActions>())
            return brokerOptionsBuilder;

        brokerOptionsBuilder.SilverbackBuilder
            .Services
            .AddScoped<MqttClientsConfigurationActions>()
            .AddTransient<IMqttNetClientFactory, MqttNetClientFactory>()
            .AddTransient<IMqttClientWrapperFactory, MqttClientWrapperFactory>()
            .AddTransient<IBrokerClientsInitializer, MqttClientsInitializer>()
            .AddTransient<MqttClientsInitializer>()
            .AddSingleton<IMqttNetLogger, DefaultMqttNetLogger>();

        return brokerOptionsBuilder;
    }
}
