// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     Wraps the <see cref="MQTTnet.MqttFactory" />.
/// </summary>
public class MqttNetClientFactory : IMqttNetClientFactory
{
    private readonly IMqttNetLogger _mqttNetLogger;

    private readonly MqttFactory _factory = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttNetClientFactory" /> class.
    /// </summary>
    /// <param name="mqttNetLogger">
    ///     The <see cref="IMqttNetLogger" />.
    /// </param>
    public MqttNetClientFactory(IMqttNetLogger mqttNetLogger)
    {
        _mqttNetLogger = mqttNetLogger;
    }

    /// <inheritdoc cref="IMqttNetClientFactory.CreateClient" />
    public IMqttClient CreateClient() => _factory.CreateMqttClient(_mqttNetLogger);
}
