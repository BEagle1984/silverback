// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     A user property to be sent with the <i>CONNECT</i> packet. It can be used to send connection related properties from the client to
///     the server.
/// </summary>
public partial record MqttUserProperty(string Name, string? Value) : IValidatableEndpointSettings
{
    /// <summary>
    ///     Gets the property name.
    /// </summary>
    public string Name { get; } = Name;

    /// <summary>
    ///     Gets the property value.
    /// </summary>
    public string? Value { get; } = Value;

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(Name))
            throw new EndpointConfigurationException("The name of a user property cannot be empty.", Name, nameof(Name));
    }

    internal MQTTnet.Packets.MqttUserProperty ToMqttNetType() =>
        new(Name, Value);
}
