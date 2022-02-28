// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration used to connect with the MQTT broker. This is actually a wrapper around the
///     <see cref="MqttClientOptions" /> from the MQTTnet library.
/// </summary>
public sealed partial record MqttClientConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.
    /// </summary>
    public string ClientId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     Gets the MQTT protocol version. The default is <see cref="MQTTnet.Formatter.MqttProtocolVersion.V500" />.
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; init; } = MqttProtocolVersion.V500;

    /// <summary>
    ///     Gets the list of user properties to be sent with the <i>CONNECT</i> packet. They can be used to send connection related
    ///     properties from the client to the server.
    /// </summary>
    public IValueReadOnlyCollection<MqttUserProperty> UserProperties { get; init; } = ValueReadOnlyCollection.Empty<MqttUserProperty>();

    /// <summary>
    ///     Gets the channel configuration (either <see cref="MqttClientTcpConfiguration" /> or <see cref="MqttClientWebSocketConfiguration" />.
    /// </summary>
    public MqttClientChannelConfiguration? Channel { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the headers (user properties) are supported according to the configured protocol version.
    /// </summary>
    internal bool AreHeadersSupported => ProtocolVersion >= MqttProtocolVersion.V500;

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(ClientId))
            throw new EndpointConfigurationException($"A {nameof(ClientId)} is required to connect with the message broker.");

        if (Channel == null)
            throw new EndpointConfigurationException("The channel configuration is required to connect with the message broker.");

        Channel.Validate();
        UserProperties.ForEach(property => property.Validate());
        WillMessage?.Validate();
    }

    internal MqttClientOptions GetMqttClientOptions()
    {
        MqttClientOptions options = MapCore();
        options.ClientId = ClientId;
        options.ProtocolVersion = ProtocolVersion;
        options.UserProperties = UserProperties.Select(property => property.ToMqttNetType()).ToList();
        options.ChannelOptions = Channel?.ToMqttNetType();
        return options;
    }
}
