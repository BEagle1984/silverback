// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public class InMemoryPartitionTests
{
    [Fact]
    public void GetAllMessages_ShouldReturnCommittedMessagesOnly()
    {
        InMemoryPartition partition = new(0, new InMemoryTopic("topic", "server", 1));
        Guid transactionalUniqueId = Guid.NewGuid();
        Guid abortedTransactionalUniqueId = Guid.NewGuid();

        partition.Add(CreateMessage(1), Guid.Empty);
        partition.Add(CreateMessage(2), transactionalUniqueId);
        partition.Add(CreateMessage(3), abortedTransactionalUniqueId);
        partition.CommitTransaction(transactionalUniqueId);
        partition.AbortTransaction(abortedTransactionalUniqueId);
        partition.Add(CreateMessage(4), Guid.NewGuid());

        partition.GetAllMessages().Select(message => message.Value![0]).ShouldBe([1, 2]);
    }

    [Fact]
    public void GetAllMessages_ShouldReturnSnapshot()
    {
        InMemoryPartition partition = new(0, new InMemoryTopic("topic", "server", 1));
        partition.Add(CreateMessage(1), Guid.Empty);

        IReadOnlyCollection<Message<byte[]?, byte[]?>> messages = partition.GetAllMessages();
        partition.Add(CreateMessage(2), Guid.Empty);

        messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAllMessages_ShouldNotThrow_WhenMessagesAreAddedConcurrently()
    {
        InMemoryPartition partition = new(0, new InMemoryTopic("topic", "server", 1));
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(5));

        Task producer = Task.Run(
            () =>
            {
                for (int i = 0; i < 10_000 && !cancellationTokenSource.IsCancellationRequested; i++)
                {
                    partition.Add(CreateMessage((byte)i), Guid.Empty);
                }
            });

        while (!producer.IsCompleted)
        {
            Should.NotThrow(() => partition.GetAllMessages());
        }

        await producer;
        partition.GetAllMessages().Count.ShouldBe(partition.TotalMessagesCount);
    }

    private static Message<byte[]?, byte[]?> CreateMessage(byte value) => new() { Value = [value] };
}
