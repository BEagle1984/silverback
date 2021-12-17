// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration of the TCP connection to the MQTT message broker.
/// </summary>
public partial record MqttClientTcpConfiguration : MqttClientChannelConfiguration
{
    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(Server))
        {
            throw new EndpointConfigurationException(
                "The server is required to connect with the message broker.",
                Server,
                nameof(Server));
        }

        if (Port <= 0)
            throw new EndpointConfigurationException("The port must be greater than zero.", Port, nameof(Port));

        Tls.Validate();
    }

    internal override MQTTnet.Client.Options.IMqttClientChannelOptions ToMqttNetType()
    {
        MQTTnet.Client.Options.MqttClientTcpOptions options = MapCore();
        options.TlsOptions = Tls.ToMqttNetType();
        return options;
    }
}
