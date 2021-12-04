// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Client;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     The factory used to create the <see cref="IMqttClient" /> instances.
/// </summary>
public interface IMqttNetClientFactory
{
    /// <summary>
    ///     Creates a new <see cref="IMqttClient" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMqttClient" />.
    /// </returns>
    IMqttClient CreateClient();
}
