// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

internal class StoredOffsetsLoader
{
    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly KafkaConsumerConfiguration _configuration;

    private readonly IServiceProvider _serviceProvider;

    private IReadOnlyCollection<KafkaOffset>? _storedOffsets;

    public StoredOffsetsLoader(
        IKafkaOffsetStoreFactory offsetStoreFactory,
        KafkaConsumerConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _configuration = Check.NotNull(configuration, nameof(configuration));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    public IReadOnlyCollection<TopicPartitionOffset> ApplyStoredOffsets(IReadOnlyCollection<TopicPartitionOffset> topicPartitionOffsets) =>
        _configuration.ClientSideOffsetStore == null
            ? topicPartitionOffsets
            : topicPartitionOffsets.Select(ApplyStoredOffset).ToList();

    public TopicPartitionOffset ApplyStoredOffset(TopicPartitionOffset topicPartitionOffset)
    {
        if (_configuration.ClientSideOffsetStore == null)
            return topicPartitionOffset;

        _storedOffsets ??= _offsetStoreFactory.GetStore(_configuration.ClientSideOffsetStore, _serviceProvider)
            .GetStoredOffsets(_configuration.GroupId ?? throw new InvalidOperationException("GroupId not set."));

        KafkaOffset? storedOffset = _storedOffsets.FirstOrDefault(offset => offset.TopicPartition == topicPartitionOffset.TopicPartition);

        if (storedOffset != null && storedOffset.Offset >= topicPartitionOffset.Offset)
            return new TopicPartitionOffset(storedOffset.TopicPartition, storedOffset.Offset + 1); // Kafka works with the next offset (+1)

        return topicPartitionOffset;
    }
}
