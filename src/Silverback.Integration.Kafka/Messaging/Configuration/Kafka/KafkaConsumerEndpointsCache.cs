// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

internal class KafkaConsumerEndpointsCache
{
    private readonly ConcurrentDictionary<TopicPartition, KafkaConsumerEndpoint> _endpoints = new();

    private readonly List<PatternSubscription> _patternSubscriptions = [];

    public KafkaConsumerEndpointsCache(KafkaConsumerConfiguration configuration)
    {
        // Match the patterns like the component selecting the subscribed topics (librdkafka or the broker)
        bool fullMatch = KafkaTopicPattern.IsFullMatchRequired(configuration.GroupProtocol);

        foreach (KafkaConsumerEndpointConfiguration endpointConfiguration in configuration.Endpoints)
        {
            IEnumerable<TopicPartition> topicPartitions =
                endpointConfiguration.TopicPartitions.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition);

            foreach (TopicPartition topicPartition in topicPartitions)
            {
                if (KafkaTopicPattern.IsPattern(topicPartition.Topic))
                {
                    _patternSubscriptions.Add(
                        new PatternSubscription(topicPartition.Topic, KafkaTopicPattern.Create(topicPartition.Topic, fullMatch), endpointConfiguration));
                }
                else
                {
                    _endpoints.TryAdd(topicPartition, new KafkaConsumerEndpoint(topicPartition, endpointConfiguration));
                }
            }
        }
    }

    public KafkaConsumerEndpoint GetEndpoint(TopicPartition topicPartition)
    {
        if (_endpoints.TryGetValue(topicPartition, out KafkaConsumerEndpoint? endpoint))
            return endpoint;

        TopicPartition anyPartition = new(topicPartition.Topic, Partition.Any);

        if (!_endpoints.TryGetValue(anyPartition, out endpoint))
        {
            // The topic names take precedence over the patterns, which are matched in the order they are configured
            PatternSubscription subscription =
                _patternSubscriptions.Find(subscription => subscription.Regex.IsMatch(topicPartition.Topic)) ??
                throw new InvalidOperationException(GetNotFoundMessage(topicPartition));

            endpoint = _endpoints.GetOrAdd(anyPartition, new KafkaConsumerEndpoint(anyPartition, subscription.EndpointConfiguration));
        }

        _endpoints.TryAdd(topicPartition, endpoint);

        return endpoint;
    }

    private string GetNotFoundMessage(TopicPartition topicPartition)
    {
        string message = $"No configuration found for the specified topic partition '{topicPartition.Topic}[{topicPartition.Partition.Value}]'.";

        if (_patternSubscriptions.Count == 0)
            return message;

        string patterns = string.Join(", ", _patternSubscriptions.Select(subscription => $"'{subscription.Pattern}'"));

        return $"{message} The topic patterns {patterns} don't match it when evaluated as .NET regular expressions.";
    }

    private sealed record PatternSubscription(string Pattern, Regex Regex, KafkaConsumerEndpointConfiguration EndpointConfiguration);
}
