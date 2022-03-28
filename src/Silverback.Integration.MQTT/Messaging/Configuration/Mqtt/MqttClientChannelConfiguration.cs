// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Client.Options;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The base class for <see cref="MqttClientTcpConfiguration" /> and <see cref="MqttClientWebSocketConfiguration" />.
/// </summary>
public abstract record MqttClientChannelConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the TLS settings.
    /// </summary>
    public MqttClientTlsConfiguration Tls { get; init; } = new();

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public abstract void Validate();

    internal abstract IMqttClientChannelOptions ToMqttNetType();
}
