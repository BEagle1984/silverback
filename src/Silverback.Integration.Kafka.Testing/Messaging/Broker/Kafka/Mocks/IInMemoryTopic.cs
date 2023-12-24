// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     A mocked topic where the messages are just stored in memory. Note that it isn't obviously possible to
///     accurately replicate the message broker behavior and this implementation is just intended for testing
///     purposes.
/// </summary>
public interface IInMemoryTopic
{
    /// <summary>
    ///     Gets the topic name.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the bootstrap servers string used to identify the target broker.
    /// </summary>
    string BootstrapServers { get; }

    /// <summary>
    ///     Gets the partitions in the topic.
    /// </summary>
    IReadOnlyList<IInMemoryPartition> Partitions { get; }

    /// <summary>
    ///     Gets the total number of messages written into all the partitions of the topic.
    /// </summary>
    int MessagesCount { get; }

    /// <summary>
    ///     Gets all messages written into all the partitions of the topic.
    /// </summary>
    /// <returns>
    ///     The messages written into the topic.
    /// </returns>
    IReadOnlyList<Message<byte[]?, byte[]?>> GetAllMessages();

    /// <summary>
    ///     Writes a message to the topic.
    /// </summary>
    /// <param name="partition">
    ///     The index of the partition to be written to.
    /// </param>
    /// <param name="message">
    ///     The message to be written.
    /// </param>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    /// <returns>
    ///     The <see cref="Offset" /> at which the message was written.
    /// </returns>
    Offset Push(int partition, Message<byte[]?, byte[]?> message, Guid transactionalUniqueId);

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    void CommitTransaction(Guid transactionalUniqueId);

    /// <summary>
    ///     Aborts the transaction.
    /// </summary>
    /// <param name="transactionalUniqueId">
    ///     The transactional unique identifier.
    /// </param>
    void AbortTransaction(Guid transactionalUniqueId);

    /// <summary>
    ///     Gets the <see cref="Offset" /> of the first message in the specified partition.
    /// </summary>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    /// <returns>
    ///     The <see cref="Offset" /> of the first message in the partition.
    /// </returns>
    Offset GetFirstOffset(Partition partition);

    /// <summary>
    ///     Gets the <see cref="Offset" /> of the latest message written to the specified partition.
    /// </summary>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    /// <returns>
    ///     The <see cref="Offset" /> of the latest message in the partition.
    /// </returns>
    Offset GetLastOffset(Partition partition);
}
