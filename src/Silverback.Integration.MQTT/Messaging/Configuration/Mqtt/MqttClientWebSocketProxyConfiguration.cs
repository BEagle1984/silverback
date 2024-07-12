// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Client;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The proxy configuration.
/// </summary>
public partial record MqttClientWebSocketProxyConfiguration : IValidatableSettings
{
    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Address))
            throw new BrokerConfigurationException("The proxy address is required.");
    }

    internal MqttClientWebSocketProxyOptions ToMqttNetType() => MapCore();
}
