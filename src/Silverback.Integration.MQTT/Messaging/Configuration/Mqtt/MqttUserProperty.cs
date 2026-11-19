// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     A user property to be sent with the <i>CONNECT</i> packet. It can be used to send connection related properties from the client to
///     the server.
/// </summary>
/// <param name="Name">
///     The property name.
/// </param>
/// <param name="Value">
///     The property value.
/// </param>
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
    public void Validate()
    {
        if (string.IsNullOrEmpty(Name))
            throw new BrokerConfigurationException($"The user property {nameof(Name)} is required.");
    }

    internal MQTTnet.Packets.MqttUserProperty ToMqttNetType() =>
        new(Name, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(Value ?? "")));
}
