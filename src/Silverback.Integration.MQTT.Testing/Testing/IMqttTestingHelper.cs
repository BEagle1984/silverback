// Copyright (c) 2024 Sergio Aquilini
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
    IProducer GetProducer(Action<MqttClientConfigurationBuilder> configurationBuilderAction);
}
