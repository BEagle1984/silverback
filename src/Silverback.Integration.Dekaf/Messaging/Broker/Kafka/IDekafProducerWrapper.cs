// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Consumer;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the underlying Dekaf producer, including the transaction currently in progress, and handles the connection
///     lifecycle.
/// </summary>
internal interface IDekafProducerWrapper : IBrokerClient
{
    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    KafkaProducerConfiguration Configuration { get; }

    /// <summary>
    ///     Gets or sets the related producer instance.
    /// </summary>
    KafkaProducer Producer { get; set; }

    /// <summary>
    ///     Produces the specified message to the specified topic and partition. The call blocks until the message is
    ///     enqueued, the outcome is reported asynchronously through either <paramref name="onDelivered" /> or
    ///     <paramref name="onFailed" />.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition.
    /// </param>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <param name="value">
    ///     The message value. A <c>null</c> value is produced as a tombstone.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="onDelivered">
    ///     The callback invoked with the offset of the message, once it has been acknowledged.
    /// </param>
    /// <param name="onFailed">
    ///     The callback invoked if the message couldn't be delivered.
    /// </param>
    void Produce(
        TopicPartition topicPartition,
        byte[]? key,
        byte[]? value,
        IReadOnlyCollection<MessageHeader> headers,
        Action<TopicPartitionOffset> onDelivered,
        Action<Exception> onFailed);

    /// <summary>
    ///     Produces the specified message to the specified topic and partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition.
    /// </param>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <param name="value">
    ///     The message value. A <c>null</c> value is produced as a tombstone.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the offset
    ///     of the produced message.
    /// </returns>
    Task<TopicPartitionOffset> ProduceAsync(
        TopicPartition topicPartition,
        byte[]? key,
        byte[]? value,
        IReadOnlyCollection<MessageHeader> headers,
        CancellationToken cancellationToken);

    /// <summary>
    ///     <para>
    ///         Initializes the transactions.
    ///     </para>
    ///     <para>
    ///         This function ensures any transactions initiated by previous instances of the producer with the same
    ///         TransactionalId are completed. If the previous instance failed with a transaction in progress the previous
    ///         transaction will be aborted.
    ///     </para>
    ///     <para>
    ///         It's a no-op if the transactions have already been initialized by this producer instance.
    ///     </para>
    /// </summary>
    void InitTransactions();

    /// <summary>
    ///     Begins a new transaction. The messages produced until it is committed or aborted are part of it.
    /// </summary>
    void BeginTransaction();

    /// <summary>
    ///     Commits the pending transaction.
    /// </summary>
    void CommitTransaction();

    /// <summary>
    ///     Aborts the pending transaction.
    /// </summary>
    void AbortTransaction();

    /// <summary>
    ///     Sends the consumed offsets to the pending transaction.
    /// </summary>
    /// <param name="offsets">
    ///     The offsets to send.
    /// </param>
    /// <param name="groupMetadata">
    ///     The consumer group metadata.
    /// </param>
    void SendOffsetsToTransaction(IReadOnlyCollection<TopicPartitionOffset> offsets, ConsumerGroupMetadata groupMetadata);
}
