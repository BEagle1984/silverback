// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Stores the latest consumed offsets in memory.
/// </summary>
public class InMemoryKafkaOffsetStore : IKafkaOffsetStore
{
    private readonly Dictionary<string, Dictionary<TopicPartition, KafkaOffset>> _offsetsByGroupId = new();

    /// <inheritdoc cref="IKafkaOffsetStore.GetStoredOffsets" />
    public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId)
    {
        lock (_offsetsByGroupId)
        {
            if (_offsetsByGroupId.TryGetValue(groupId, out Dictionary<TopicPartition, KafkaOffset>? offsetsByTopicPartition))
                return offsetsByTopicPartition.Values;

            return Array.Empty<KafkaOffset>();
        }
    }

    /// <inheritdoc cref="IKafkaOffsetStore.StoreOffsetsAsync" />
    public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, SilverbackContext? context = null)
    {
        Check.NotNull(offsets, nameof(offsets));

        lock (_offsetsByGroupId)
        {
            Dictionary<TopicPartition, KafkaOffset> offsetsByTopicPartition =
                _offsetsByGroupId.GetOrAdd(groupId, _ => new Dictionary<TopicPartition, KafkaOffset>());

            foreach (KafkaOffset offset in offsets)
            {
                offsetsByTopicPartition[offset.TopicPartition] = offset;
            }
        }

        return Task.CompletedTask;
    }
}
