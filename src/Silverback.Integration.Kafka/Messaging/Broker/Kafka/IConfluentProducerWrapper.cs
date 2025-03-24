// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the underlying <see cref="Producer{TKey,TValue}" /> and handles the connection lifecycle.
/// </summary>
public interface IConfluentProducerWrapper : IBrokerClient
{
    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    KafkaProducerConfiguration Configuration { get; }

    /// <summary>
    ///     Produces the specified message to the specified topic and partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition.
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="deliveryHandler">
    ///     The <see cref="DeliveryReport{TKey,TValue}" /> handler.
    /// </param>
    void Produce(TopicPartition topicPartition, Message<byte[]?, byte[]?> message, Action<DeliveryReport<byte[]?, byte[]?>> deliveryHandler);

    /// <summary>
    ///     Produces the specified message to the specified topic and partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition.
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="DeliveryResult{TKey,TValue}" />.
    /// </returns>
    Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
        TopicPartition topicPartition,
        Message<byte[]?, byte[]?> message,
        CancellationToken cancellationToken);

    /// <summary>
    ///     <para>
    ///         Initialize the transactions.
    ///     </para>
    ///     <para>
    ///         This function ensures any transactions initiated by previous instances of the producer with the same TransactionalId are
    ///         completed. If the previous instance failed with a transaction in progress the previous transaction will be aborted.
    ///     </para>
    ///     <para>
    ///         This function needs to be called before any other transactional or produce functions are called when the TransactionalId is
    ///         configured.
    ///     </para>
    /// </summary>
    public void InitTransactions();

    /// <summary>
    ///     Begins a new transaction.
    /// </summary>
    public void BeginTransaction();

    /// <summary>
    ///     Commits the pending transaction.
    /// </summary>
    public void CommitTransaction();

    /// <summary>
    ///     Aborts the pending transaction.
    /// </summary>
    public void AbortTransaction();

    /// <summary>
    ///     Sends the consumed offsets to the transaction.
    /// </summary>
    /// <param name="offsets">
    ///     The offsets to send.
    /// </param>
    /// <param name="groupMetadata">
    ///     The consumer group metadata.
    /// </param>
    void SendOffsetsToTransaction(IReadOnlyCollection<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata);
}
