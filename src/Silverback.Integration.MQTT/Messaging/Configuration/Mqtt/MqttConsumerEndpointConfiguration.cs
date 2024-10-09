// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using MQTTnet.Protocol;
using Silverback.Collections;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The MQTT consumer configuration.
/// </summary>
public sealed record MqttConsumerEndpointConfiguration : ConsumerEndpointConfiguration
{
    private readonly IValueReadOnlyCollection<string> _topics = ValueReadOnlyCollection.Empty<string>();

    /// <summary>
    ///     Gets the name of the topics or the topic filter strings.
    /// </summary>
    public IValueReadOnlyCollection<string> Topics
    {
        get => _topics;
        init
        {
            _topics = value;

            if (value != null)
                RawName = string.Join(",", value);
        }
    }

    /// <summary>
    ///     Gets the quality of service level (at most once, at least once or exactly once).
    ///     The default is <see cref="MqttQualityOfServiceLevel.AtMostOnce" />.
    /// </summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; init; }

    /// <inheritdoc cref="ConsumerEndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Topics == null || Topics.Count == 0)
            throw new BrokerConfigurationException("At least 1 topic must be specified.");

        if (Topics.Any(string.IsNullOrEmpty))
            throw new BrokerConfigurationException("A topic name cannot be null or empty.");

        if (Batch is { Size: > 1 })
            throw new BrokerConfigurationException("Batch processing is currently not supported for MQTT.");
    }
}
