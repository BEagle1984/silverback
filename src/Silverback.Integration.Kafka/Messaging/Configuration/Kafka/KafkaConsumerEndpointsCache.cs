// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

internal class KafkaConsumerEndpointsCache
{
    private readonly ConcurrentDictionary<TopicPartition, KafkaConsumerEndpoint> _endpoints = new();

    public KafkaConsumerEndpointsCache(KafkaConsumerConfiguration configuration)
    {
        foreach (KafkaConsumerEndpointConfiguration endpointConfiguration in configuration.Endpoints)
        {
            IEnumerable<TopicPartition> topicPartitions =
                endpointConfiguration.TopicPartitions.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition);

            foreach (TopicPartition topicPartition in topicPartitions)
            {
                _endpoints.TryAdd(topicPartition, new KafkaConsumerEndpoint(topicPartition, endpointConfiguration));
            }
        }
    }

    public KafkaConsumerEndpoint GetEndpoint(TopicPartition topicPartition)
    {
        if (_endpoints.TryGetValue(topicPartition, out KafkaConsumerEndpoint? configuration))
            return configuration;

        if (_endpoints.TryGetValue(new TopicPartition(topicPartition.Topic, Partition.Any), out configuration))
        {
            _endpoints.TryAdd(topicPartition, configuration);
            return configuration;
        }

        throw new InvalidOperationException($"No configuration found for the specified topic partition '{topicPartition.Topic}[{topicPartition.Partition.Value}]'.");
    }
}
