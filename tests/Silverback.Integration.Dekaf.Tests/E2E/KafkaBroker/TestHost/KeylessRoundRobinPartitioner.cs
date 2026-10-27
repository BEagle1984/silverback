// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Dekaf.Producer;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     Spreads the messages without key over the partitions in round-robin, like the mocked Kafka the ported tests
///     were written for (librdkafka and Dekaf pick a random or sticky partition instead). The messages with a key
///     are partitioned by its CRC32 hash, like librdkafka.
/// </summary>
internal sealed class KeylessRoundRobinPartitioner : IPartitioner
{
    private readonly ConsistentPartitioner _keyedPartitioner = new();

    private readonly ConcurrentDictionary<string, int> _nextPartitions = new(StringComparer.Ordinal);

    public int Partition(string topic, ReadOnlySpan<byte> key, bool keyIsNull, int partitionCount)
    {
        if (!keyIsNull)
            return _keyedPartitioner.Partition(topic, key, keyIsNull, partitionCount);

        int counter = _nextPartitions.AddOrUpdate(topic, 0, (_, previous) => previous + 1);
        return counter % partitionCount;
    }
}
