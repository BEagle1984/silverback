// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration of the TCP connection to the MQTT message broker.
/// </summary>
public partial record MqttClientTcpConfiguration : MqttClientChannelConfiguration
{
    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public override void Validate()
    {
        if (RemoteEndpoint == null)
            throw new BrokerConfigurationException("The remote endpoint is required to connect with the message broker.");

        if (Tls == null)
            throw new BrokerConfigurationException("The TLS configuration is required.");

        Tls.Validate();
    }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => RemoteEndpoint?.ToString() ?? "[undefined remote endpoint]";

    internal override IMqttClientChannelOptions ToMqttNetType()
    {
        MqttClientTcpOptions options = MapCore();
        options.TlsOptions = Tls.ToMqttNetType();
        return options;
    }
}
