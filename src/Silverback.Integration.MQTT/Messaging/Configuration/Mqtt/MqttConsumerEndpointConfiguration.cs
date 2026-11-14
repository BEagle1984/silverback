// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The MQTT consumer configuration.
/// </summary>
public sealed record MqttConsumerEndpointConfiguration : ConsumerEndpointConfiguration
{
    /// <summary>
    ///     Gets the name of the topics or the topic filter strings.
    /// </summary>
    public IValueReadOnlyCollection<string> Topics
    {
        get;
        init
        {
            field = value;

            if (value != null)
                RawName = string.Join(",", value);
        }
    } = ValueReadOnlyCollection.Empty<string>();

    /// <summary>
    ///     Gets the quality of service level (at most once, at least once, or exactly once).
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

        Topics.ForEach(ValidateTopicFilter);
    }

    private static void ValidateTopicFilter(string topic)
    {
        string filter = topic;

        if (ParsedTopic.TrySplitSharedSubscription(topic, out string? group, out string? sharedFilter))
        {
            if (group.Length == 0 || group.Contains('+', StringComparison.Ordinal) || group.Contains('#', StringComparison.Ordinal) ||
                sharedFilter.Length == 0)
            {
                throw new BrokerConfigurationException(
                    $"The shared subscription '{topic}' is not valid. " +
                    "The expected format is '$share/{ShareName}/{TopicFilter}', with a share name without wildcards.");
            }

            filter = sharedFilter;
        }

        string[] levels = filter.Split('/');

        for (int i = 0; i < levels.Length; i++)
        {
            bool isValidLevel = levels[i] switch
            {
                "#" => i == levels.Length - 1,
                "+" => true,
                _ => !levels[i].Contains('+', StringComparison.Ordinal) && !levels[i].Contains('#', StringComparison.Ordinal)
            };

            if (!isValidLevel)
            {
                throw new BrokerConfigurationException(
                    $"The topic filter '{topic}' is not valid. " +
                    "The wildcards '+' and '#' must occupy an entire level and '#' must be the last level.");
            }
        }
    }
}
