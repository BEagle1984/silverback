// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddMqttEndpoints</c> method to the <see cref="BrokerClientsConfigurationBuilder" />.
/// </summary>
public static class EndpointsConfigurationBuilderAddMqttEndpointsExtensions
{
    /// <summary>
    ///     Adds the MQTT endpoints.
    /// </summary>
    /// <param name="brokerClientsConfigurationBuilder">
    ///     The <see cref="BrokerClientsConfigurationBuilder" />.
    /// </param>
    /// <param name="mqttEndpointsBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientsConfigurationBuilder" />,
    ///     configures the connection to the message broker and adds the inbound and outbound endpoints.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerClientsConfigurationBuilder AddMqttClients(
        this BrokerClientsConfigurationBuilder brokerClientsConfigurationBuilder,
        Action<MqttClientsConfigurationBuilder> mqttEndpointsBuilderAction)
    {
        Check.NotNull(brokerClientsConfigurationBuilder, nameof(brokerClientsConfigurationBuilder));
        Check.NotNull(mqttEndpointsBuilderAction, nameof(mqttEndpointsBuilderAction));

        MqttClientsConfigurationBuilder mqttClientsBuilder = new();
        mqttEndpointsBuilderAction.Invoke(mqttClientsBuilder);
        MergeableActionCollection<MqttClientConfigurationBuilder> configurationActions = mqttClientsBuilder.GetConfigurationActions();

        MqttClientsConfigurationActions globalConfigurationActions =
            brokerClientsConfigurationBuilder.ServiceProvider.GetRequiredService<MqttClientsConfigurationActions>();

        globalConfigurationActions.ConfigurationActions.Append(configurationActions);

        return brokerClientsConfigurationBuilder;
    }
}
