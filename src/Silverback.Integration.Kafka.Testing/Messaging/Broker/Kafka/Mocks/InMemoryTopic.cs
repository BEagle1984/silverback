// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class InMemoryTopic : IInMemoryTopic
{
    private readonly List<InMemoryPartition> _partitions;

    public InMemoryTopic(string name, string bootstrapServers, int partitions)
    {
        Name = Check.NotEmpty(name, nameof(name));
        BootstrapServers = Check.NotEmpty(bootstrapServers, nameof(bootstrapServers));

        if (partitions < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(partitions),
                partitions,
                "The number of partition must be a positive number greater or equal to 1.");
        }

        _partitions = new List<InMemoryPartition>(
            Enumerable.Range(0, partitions)
                .Select(i => new InMemoryPartition(i, this)));
    }

    public string Name { get; }

    public string BootstrapServers { get; }

    public IReadOnlyList<IInMemoryPartition> Partitions => _partitions;

    public int MessagesCount =>
        _partitions.Sum(partition => partition.Messages.Count);

    public IReadOnlyList<Message<byte[]?, byte[]?>> GetAllMessages() =>
        _partitions.SelectMany(partition => partition.Messages).ToList();

    public Offset Push(int partition, Message<byte[]?, byte[]?> message) =>
        _partitions[partition].Add(message);

    public Offset GetFirstOffset(Partition partition)
        => _partitions[partition].FirstOffset;

    public Offset GetLastOffset(Partition partition)
        => _partitions[partition].LastOffset;
}
