// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The proxy configuration.
/// </summary>
public partial record MqttClientWebSocketProxyConfiguration : IValidatableEndpointSettings
{
    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Address))
            throw new EndpointConfigurationException("The proxy address is required.");
    }

    internal MQTTnet.Client.Options.MqttClientWebSocketProxyOptions ToMqttNetType() => MapCore();
}
