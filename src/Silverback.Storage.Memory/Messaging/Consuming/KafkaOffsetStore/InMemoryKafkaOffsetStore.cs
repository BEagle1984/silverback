// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
    private readonly Dictionary<string, Dictionary<TopicPartition, KafkaOffset>> _offsetsByGroupId = [];

    private readonly System.Threading.Lock _syncLock = new();

    /// <inheritdoc cref="IKafkaOffsetStore.GetStoredOffsets" />
    public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId)
    {
        lock (_syncLock)
        {
            if (_offsetsByGroupId.TryGetValue(groupId, out Dictionary<TopicPartition, KafkaOffset>? offsetsByTopicPartition))
                return [.. offsetsByTopicPartition.Values];

            return [];
        }
    }

    /// <inheritdoc cref="IKafkaOffsetStore.StoreOffsetsAsync" />
    public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null)
    {
        Check.NotNull(offsets);

        lock (_syncLock)
        {
            Dictionary<TopicPartition, KafkaOffset> offsetsByTopicPartition =
                _offsetsByGroupId.GetOrAdd(groupId, _ => []);

            foreach (KafkaOffset offset in offsets)
            {
                offsetsByTopicPartition[offset.TopicPartition] = offset;
            }
        }

        return Task.CompletedTask;
    }
}
