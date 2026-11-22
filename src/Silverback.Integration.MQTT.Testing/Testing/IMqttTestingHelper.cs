// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Testing;

/// <inheritdoc cref="ITestingHelper" />
public interface IMqttTestingHelper : ITestingHelper
{
    /// <summary>
    ///     Gets the <see cref="IClientSession" /> of the specified client.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked MQTT broker only. See
    ///     <see cref="SilverbackBuilderMqttTestingExtensions.UseMockedMqtt" /> or
    ///     <see cref="BrokerOptionsBuilderMqttTestingExtensions.AddMockedMqtt" />.
    /// </remarks>
    /// <param name="clientId">
    ///     The client id.
    /// </param>
    /// <returns>
    ///     The <see cref="IClientSession" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     No session (or more than one) exists for the specified client.
    /// </exception>
    IClientSession GetClientSession(string clientId);

    /// <summary>
    ///     Gets the messages that have been published to the specified topic.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked MQTT broker only. See
    ///     <see cref="SilverbackBuilderMqttTestingExtensions.UseMockedMqtt" /> or
    ///     <see cref="BrokerOptionsBuilderMqttTestingExtensions.AddMockedMqtt" />.
    /// </remarks>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <returns>
    ///     The messages published to the topic.
    /// </returns>
    IReadOnlyList<MqttApplicationMessage> GetMessages(string topic);

    /// <summary>
    ///     Gets a new producer with the specified configuration.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    /// <exception cref="BrokerConfigurationException">
    ///     The client configuration is not valid.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The MQTT integration hasn't been added (<c>AddMqtt</c> hasn't been called), the configuration doesn't define exactly one
    ///     producer endpoint, or a producer endpoint with the same name has already been added.
    /// </exception>
    IProducer GetProducer(Action<MqttClientConfigurationBuilder> configurationBuilderAction);
}
