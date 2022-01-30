// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(Uri))
            throw new EndpointConfigurationException("The URI is required to connect with the message broker.", Uri, nameof(Uri));

        if (Tls == null)
            throw new EndpointConfigurationException("The TLS configuration is required.", Tls, nameof(Tls));

        Proxy?.Validate();
        Tls.Validate();
    }

    internal override MQTTnet.Client.Options.IMqttClientChannelOptions ToMqttNetType()
    {
        MQTTnet.Client.Options.MqttClientWebSocketOptions options = MapCore();
        options.TlsOptions = Tls.ToMqttNetType();
        return options;
    }
}