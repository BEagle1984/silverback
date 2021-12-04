// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet.Client;
using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     Creates and stores the MQTT clients in order to create a single connection per each
///     <see cref="MqttClientConfiguration" />.
/// </summary>
internal interface IMqttClientsCache : IDisposable
{
    /// <summary>
    ///     Returns an <see cref="IMqttClient" /> for the specified <see cref="MqttProducer" />. Returns an
    ///     existing instance if one exists that is compatible with the endpoint configuration.
    /// </summary>
    /// <param name="producer">
    ///     The <see cref="MqttProducer" />.
    /// </param>
    /// <returns>
    ///     An <see cref="MqttClientWrapper" /> containing the <see cref="IMqttClient" />.
    /// </returns>
    MqttClientWrapper GetClient(MqttProducer producer);

    /// <summary>
    ///     Returns an <see cref="IMqttClient" /> for the specified <see cref="MqttProducer" />. Returns an
    ///     existing instance only if it is compatible with the endpoint configuration and no other consumer is
    ///     bound to it already.
    /// </summary>
    /// <param name="consumer">
    ///     The <see cref="MqttConsumer" />.
    /// </param>
    /// <returns>
    ///     An <see cref="MqttClientWrapper" /> containing the <see cref="IMqttClient" />.
    /// </returns>
    MqttClientWrapper GetClient(MqttConsumer consumer);
}
