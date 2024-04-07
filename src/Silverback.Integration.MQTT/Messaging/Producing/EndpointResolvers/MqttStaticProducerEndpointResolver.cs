// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Statically resolves to the same target topic for every message being produced.
/// </summary>
public sealed class MqttStaticProducerEndpointResolver
    : StaticProducerEndpointResolver<MqttProducerEndpoint, MqttProducerEndpointConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttStaticProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    public MqttStaticProducerEndpointResolver(string topic)
        : base(Check.NotNullOrEmpty(topic, nameof(topic)))
    {
        Topic = topic;
    }

    /// <summary>
    ///     Gets the target topic.
    /// </summary>
    public string Topic { get; }

    /// <inheritdoc cref="StaticProducerEndpointResolver{TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override MqttProducerEndpoint GetEndpointCore(MqttProducerEndpointConfiguration configuration) => new(Topic, configuration);
}
