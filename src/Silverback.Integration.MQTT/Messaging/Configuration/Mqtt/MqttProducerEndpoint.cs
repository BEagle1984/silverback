// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The MQTT topic where the message must be produced to.
/// </summary>
public record MqttProducerEndpoint : ProducerEndpoint<MqttProducerEndpointConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttProducerEndpoint" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    public MqttProducerEndpoint(string topic, MqttProducerEndpointConfiguration configuration)
        : base(Check.NotNull(topic, nameof(topic)), configuration)
    {
        Topic = topic;
    }

    /// <summary>
    ///     Gets the target topic.
    /// </summary>
    public string Topic { get; }
}
