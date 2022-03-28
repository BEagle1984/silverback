// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Client.Options;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration of the TCP connection to the MQTT message broker.
/// </summary>
public partial record MqttClientTcpConfiguration : MqttClientChannelConfiguration
{
    /// <inheritdoc cref="IValidatableSettings.Validate" />
    // TODO: Test
    public override void Validate()
    {
        if (string.IsNullOrEmpty(Server))
            throw new BrokerConfigurationException("The server is required to connect with the message broker.");

        if (Port <= 0)
            throw new BrokerConfigurationException("The port must be greater than zero.");

        Tls.Validate();
    }

    /// <inheritdoc cref="object.ToString" />
    public override string ToString() => $"{Server}:{GetPort()}";

    internal override IMqttClientChannelOptions ToMqttNetType()
    {
        MqttClientTcpOptions options = MapCore();
        options.TlsOptions = Tls.ToMqttNetType();
        return options;
    }

    private int GetPort() => Port ?? (Tls.UseTls ? 8883 : 1883);
}
