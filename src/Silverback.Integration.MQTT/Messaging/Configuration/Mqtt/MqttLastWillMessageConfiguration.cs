// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration of the last will message to be sent when the client disconnects ungracefully.
/// </summary>
public record MqttLastWillMessageConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    ///     Gets the correlation data.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Same as wrapped library")]
    public byte[]? CorrelationData { get; init; }

    /// <summary>
    ///     Gets the number of seconds to wait before sending the last will message. If the client reconnects between this interval the message
    ///     will not be sent.
    /// </summary>
    public uint Delay { get; init; }

    /// <summary>
    ///     Gets the message expiry interval.
    /// </summary>
    public uint MessageExpiration { get; init; }

    /// <summary>
    ///     Gets the message payload.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed")]
    public byte[]? Payload { get; init; }

    /// <summary>
    ///     Gets the payload format indicator.
    /// </summary>
    public MqttPayloadFormatIndicator PayloadFormatIndicator { get; init; }

    /// <summary>
    ///     Gets the quality of service level (at most once, at least once or exactly once).
    ///     The default is <see cref="MqttQualityOfServiceLevel.AtMostOnce" />.
    /// </summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; init; }

    /// <summary>
    ///     Gets the response topic.
    /// </summary>
    public string? ResponseTopic { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the message have to be sent with the retain flag, causing them to be persisted on the broker.
    ///     The default is <c>false</c>.
    /// </summary>
    public bool Retain { get; init; }

    /// <summary>
    ///     Gets the target topic.
    /// </summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the user properties of the will message.
    /// </summary>
    public IValueReadOnlyCollection<MqttUserProperty> UserProperties { get; init; } = ValueReadOnlyCollection.Empty<MqttUserProperty>();

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(Topic))
            throw new BrokerConfigurationException("The topic is required.");
    }

    internal void MapToMqttNetType(MqttClientOptions options)
    {
        options.WillContentType = ContentType;
        options.WillCorrelationData = CorrelationData;
        options.WillMessageExpiryInterval = MessageExpiration;
        options.WillPayload = Payload;
        options.WillPayloadFormatIndicator = PayloadFormatIndicator;
        options.WillQualityOfServiceLevel = QualityOfServiceLevel;
        options.WillResponseTopic = ResponseTopic;
        options.WillRetain = Retain;
        options.WillTopic = Topic;
        options.WillUserProperties = UserProperties.Select(property => property.ToMqttNetType()).ToList();
        options.WillDelayInterval = Delay;
    }
}
