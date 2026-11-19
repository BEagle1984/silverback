// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet;
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
    public override string ToString() => Uri ?? "[undefined uri]";

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(Uri))
            throw new BrokerConfigurationException($"Channel.{nameof(Uri)} is required.");

        if (Tls == null)
            throw new BrokerConfigurationException($"Channel.{nameof(Tls)} is required.");

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
