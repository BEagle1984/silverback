// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

// TODO: Test
internal class StoredOffsetsLoader
{
    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly KafkaConsumerConfiguration _configuration;

    private IReadOnlyCollection<KafkaOffset>? _storedOffsets;

    public StoredOffsetsLoader(IKafkaOffsetStoreFactory offsetStoreFactory, KafkaConsumerConfiguration configuration)
    {
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _configuration = Check.NotNull(configuration, nameof(configuration));
    }

    public IReadOnlyCollection<TopicPartitionOffset> ApplyStoredOffsets(IReadOnlyCollection<TopicPartitionOffset> topicPartitionOffsets) =>
        _configuration.ClientSideOffsetStore == null
            ? topicPartitionOffsets
            : topicPartitionOffsets.Select(ApplyStoredOffset).ToList();

    public TopicPartitionOffset ApplyStoredOffset(TopicPartitionOffset topicPartitionOffset)
    {
        if (_configuration.ClientSideOffsetStore == null)
            return topicPartitionOffset;

        _storedOffsets ??= _offsetStoreFactory.GetStore(_configuration.ClientSideOffsetStore).GetStoredOffsets(_configuration.GroupId);

        KafkaOffset? storedOffset = _storedOffsets.FirstOrDefault(offset => offset.TopicPartition == topicPartitionOffset.TopicPartition);

        if (storedOffset != null && storedOffset.Offset >= topicPartitionOffset.Offset) // TODO: Test edge case (>=)
            return new TopicPartitionOffset(storedOffset.TopicPartition, storedOffset.Offset + 1); // Kafka works with the next offset (+1)

        return topicPartitionOffset;
    }
}
