// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Threading;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Tests.Integration.E2E.TestTypes;

public class TestKafkaOffsetCommittedCallback : IKafkaOffsetCommittedCallback
{
    private int _callsCount;

    public ConcurrentDictionary<TopicPartition, Offset> Offsets { get; } = new();

    public int CallsCount => _callsCount;

    public void OnOffsetsCommitted(CommittedOffsets offsets, KafkaConsumer consumer)
    {
        Interlocked.Increment(ref _callsCount);

        foreach (TopicPartitionOffsetError? offset in offsets.Offsets)
        {
            Offsets.AddOrUpdate(offset.TopicPartition, _ => offset.Offset, (_, _) => offset.Offset);
        }
    }
}
