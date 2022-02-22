// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using MQTTnet;
using MQTTnet.Protocol;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration of the last will message to be sent when the client disconnects ungracefully.
/// </summary>
public record MqttLastWillMessageConfiguration : IValidatableEndpointSettings
{
    /// <summary>
    ///     Gets the target topic.
    /// </summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the message payload.
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
    public byte[]? Payload { get; init; }

    /// <summary>
    ///     Gets the quality of service level (at most once, at least once or exactly once).
    ///     The default is <see cref="MqttQualityOfServiceLevel.AtMostOnce" />.
    /// </summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message have to be sent with the retain flag, causing them to be persisted on the broker.
    ///     The default is <c>false</c>.
    /// </summary>
    public bool Retain { get; init; }

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(Topic))
            throw new EndpointConfigurationException("The topic is required.");
    }

    internal MqttApplicationMessage ToMqttNetType() =>
        new()
        {
            Topic = Topic,
            Payload = Payload,
            QualityOfServiceLevel = QualityOfServiceLevel,
            Retain = Retain
        };
}
