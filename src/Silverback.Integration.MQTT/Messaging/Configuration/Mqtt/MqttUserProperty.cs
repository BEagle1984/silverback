// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     A user property to be sent with the <i>CONNECT</i> packet. It can be used to send connection related properties from the client to
///     the server.
/// </summary>
public record MqttUserProperty(string Name, string? Value) : IValidatableSettings
{
    /// <summary>
    ///     Gets the property name.
    /// </summary>
    public string Name { get; } = Name;

    /// <summary>
    ///     Gets the property value.
    /// </summary>
    public string? Value { get; } = Value;

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    // TODO: Test
    public void Validate()
    {
        if (string.IsNullOrEmpty(Name))
            throw new BrokerConfigurationException("The name of a user property cannot be empty.");
    }

    internal MQTTnet.Packets.MqttUserProperty ToMqttNetType() =>
        new(Name, Value);
}
