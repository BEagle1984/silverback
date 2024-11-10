// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Contains the constants with the names of the Kafka specific message headers used by Silverback.
/// </summary>
public static class MqttMessageHeaders
{
    /// <summary>
    ///     The header containing the topic the response message should be published to. This is mapped to the MQTT 5 response topic property.
    /// </summary>
    public const string ResponseTopic = DefaultMessageHeaders.InternalHeadersPrefix + "mqtt5-response-topic";

    /// <summary>
    ///     The header containing the correlation data. This is mapped to the MQTT 5 correlation data property.
    /// </summary>
    public const string CorrelationData = DefaultMessageHeaders.InternalHeadersPrefix + "mqtt5-correlation-data";

    /// <summary>
    ///     The header containing the dynamic destination topic the message should be produced to.
    /// </summary>
    public const string DestinationTopic = DefaultMessageHeaders.InternalHeadersPrefix + "mqtt-destination-topic";
}
