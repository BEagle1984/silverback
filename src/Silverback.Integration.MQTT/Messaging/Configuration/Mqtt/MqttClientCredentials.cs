// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The credentials used to connect to the MQTT broker.
/// </summary>
public partial record MqttClientCredentials : IValidatableSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientCredentials" /> class.
    /// </summary>
    /// <param name="username">
    ///     The username.
    /// </param>
    /// <param name="password">
    ///     The password.
    /// </param>
    public MqttClientCredentials(string? username, byte[]? password)
    {
        Username = username;
        Password = password;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientCredentials" /> class.
    /// </summary>
    /// <param name="username">
    ///     The username.
    /// </param>
    /// <param name="password">
    ///     The password.
    /// </param>
    public MqttClientCredentials(string? username, string? password)
    {
        Username = username;

        if (password != null)
            Password = Encoding.UTF8.GetBytes(password);
    }

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        // Nothing to validate
    }

    internal MQTTnet.Client.Options.MqttClientCredentials ToMqttNetType() => MapCore();
}
