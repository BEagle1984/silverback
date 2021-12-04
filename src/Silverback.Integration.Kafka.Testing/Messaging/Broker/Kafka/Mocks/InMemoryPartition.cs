// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class InMemoryPartition : IInMemoryPartition
{
    private readonly List<Message<byte[]?, byte[]?>> _messages = new();

    public InMemoryPartition(in int index, InMemoryTopic topic)
    {
        Partition = new Partition(index);
        Topic = topic;
    }

    public Partition Partition { get; }

    public InMemoryTopic Topic { get; }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
    public IReadOnlyCollection<Message<byte[]?, byte[]?>> Messages => _messages;

    public Offset FirstOffset { get; private set; } = Offset.Unset;

    public Offset LastOffset { get; private set; } = Offset.Unset;

    public int TotalMessagesCount { get; private set; }

    public Offset Add(Message<byte[]?, byte[]?> message)
    {
        lock (_messages)
        {
            if (_messages.Count == 0)
                LastOffset = FirstOffset = new Offset(0);
            else
                LastOffset++;

            _messages.Add(message);

            TotalMessagesCount++;

            return new Offset(LastOffset);
        }
    }

    public bool TryPull(Offset offset, out ConsumeResult<byte[]?, byte[]?>? result)
    {
        lock (_messages)
        {
            if (offset.Value > LastOffset || _messages.Count == 0)
            {
                result = null;
                return false;
            }

            result = new ConsumeResult<byte[]?, byte[]?>
            {
                IsPartitionEOF = false,
                Message = _messages[(int)(offset.Value - Math.Max(0, FirstOffset.Value))],
                Offset = offset,
                Partition = Partition,
                Topic = Topic.Name
            };
        }

        return true;
    }
}
