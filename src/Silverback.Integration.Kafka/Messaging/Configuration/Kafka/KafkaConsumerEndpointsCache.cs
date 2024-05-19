// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration.Kafka;

internal class KafkaConsumerEndpointsCache
{
    private readonly ConcurrentDictionary<TopicPartition, CachedEndpoint> _endpoints = new();

    public KafkaConsumerEndpointsCache(KafkaConsumerConfiguration configuration)
    {
        foreach (KafkaConsumerEndpointConfiguration endpointConfiguration in configuration.Endpoints)
        {
            IEnumerable<TopicPartition> topicPartitions =
                endpointConfiguration.TopicPartitions.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition);

            foreach (TopicPartition topicPartition in topicPartitions)
            {
                _endpoints.TryAdd(topicPartition, GetEndpoint(topicPartition, endpointConfiguration));
            }
        }
    }

    public CachedEndpoint GetEndpoint(TopicPartition topicPartition)
    {
        if (_endpoints.TryGetValue(topicPartition, out CachedEndpoint? configuration))
            return configuration;

        if (_endpoints.TryGetValue(new TopicPartition(topicPartition.Topic, Partition.Any), out configuration))
        {
            _endpoints.TryAdd(topicPartition, configuration);
            return configuration;
        }

        throw new InvalidOperationException($"No configuration found for the specified topic partition '{topicPartition.Topic}[{topicPartition.Partition.Value}]'.");
    }

    private static CachedEndpoint GetEndpoint(TopicPartition topicPartition, KafkaConsumerEndpointConfiguration configuration) =>
        new(new KafkaConsumerEndpoint(topicPartition, configuration), GetKafkaDeserializer(configuration.Deserializer));

    private static IKafkaMessageDeserializer GetKafkaDeserializer(IMessageDeserializer deserializer) =>
        deserializer as IKafkaMessageDeserializer ?? new DefaultKafkaMessageDeserializer(deserializer);

    public record CachedEndpoint(KafkaConsumerEndpoint Endpoint, IKafkaMessageDeserializer Deserializer);
}
