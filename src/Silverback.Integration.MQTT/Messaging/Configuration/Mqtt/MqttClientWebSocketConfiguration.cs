// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Client;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration of the websocket connection to the MQTT message broker.
/// </summary>
public partial record MqttClientWebSocketConfiguration : MqttClientChannelConfiguration
{
    /// <summary>
    ///     Gets the proxy configuration.
    /// </summary>
    public MqttClientWebSocketProxyConfiguration? Proxy { get; init; }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => Uri ?? string.Empty;

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    // TODO: Test
    public override void Validate()
    {
        if (string.IsNullOrEmpty(Uri))
            throw new BrokerConfigurationException("The URI is required to connect with the message broker.");

        if (Tls == null)
            throw new BrokerConfigurationException("The TLS configuration is required.");

        Proxy?.Validate();
        Tls.Validate();
    }

    internal override IMqttClientChannelOptions ToMqttNetType()
    {
        MqttClientWebSocketOptions options = MapCore();
        options.TlsOptions = Tls.ToMqttNetType();
        return options;
    }
}
