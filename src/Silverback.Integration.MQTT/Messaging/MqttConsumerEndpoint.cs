// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The MQTT topic from which the message was consumed.
/// </summary>
public record MqttConsumerEndpoint : ConsumerEndpoint<MqttConsumerConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConsumerEndpoint" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    public MqttConsumerEndpoint(string topic, MqttConsumerConfiguration configuration)
        : base(Check.NotNull(topic, nameof(topic)), configuration)
    {
        Topic = topic;
    }

    /// <summary>
    ///     Gets the source topic.
    /// </summary>
    public string Topic { get; }
}
