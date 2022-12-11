// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the underlying <see cref="Producer{TKey,TValue}" /> and handles the connection lifecycle.
/// </summary>
public interface IConfluentProducerWrapper : IBrokerClient
{
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
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="DeliveryResult{TKey,TValue}" />.
    /// </returns>
    Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(TopicPartition topicPartition, Message<byte[]?, byte[]?> message);
}
