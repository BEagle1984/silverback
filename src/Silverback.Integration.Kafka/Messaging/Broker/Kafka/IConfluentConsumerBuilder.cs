// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The <see cref="IConsumer{TKey,TValue}" /> builder used by the <see cref="KafkaConsumer" />.
/// </summary>
public interface IConfluentConsumerBuilder
{
    /// <summary>
    ///     Sets the consumer configuration.
    /// </summary>
    /// <param name="config">
    ///     The configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetConfig(ConsumerConfig config);

    /// <summary>
    ///     Sets the handler to call on statistics events.
    /// </summary>
    /// <param name="statisticsHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetStatisticsHandler(Action<IConsumer<byte[]?, byte[]?>, string> statisticsHandler);

    /// <summary>
    ///     Sets the handler to call on error events.
    /// </summary>
    /// <param name="errorHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetErrorHandler(Action<IConsumer<byte[]?, byte[]?>, Error> errorHandler);

    /// <summary>
    ///     Sets the handler to call on partitions assigned events.
    /// </summary>
    /// <param name="partitionsAssignedHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetPartitionsAssignedHandler(Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionsAssignedHandler);

    /// <summary>
    ///     Sets the handler to call on partitions assigned events.
    /// </summary>
    /// <param name="partitionsAssignedHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetPartitionsAssignedHandler(Action<IConsumer<byte[]?, byte[]?>, List<TopicPartition>> partitionsAssignedHandler);

    /// <summary>
    ///     Sets the handler to call on partitions revoked events.
    /// </summary>
    /// <param name="partitionsRevokedHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetPartitionsRevokedHandler(Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> partitionsRevokedHandler);

    /// <summary>
    ///     Sets the handler to call on partitions revoked events.
    /// </summary>
    /// <param name="partitionsRevokedHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetPartitionsRevokedHandler(Action<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>> partitionsRevokedHandler);

    /// <summary>
    ///     Sets the handler to call on offsets committed events.
    /// </summary>
    /// <param name="offsetsCommittedHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    IConfluentConsumerBuilder SetOffsetsCommittedHandler(Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets> offsetsCommittedHandler);

    /// <summary>
    ///     Set the handler to call when there is information available to be logged. If not specified, a default
    ///     callback that writes to stderr will be used.
    /// </summary>
    /// <param name="logHandler">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     The <see cref="IConfluentProducerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public IConfluentConsumerBuilder SetLogHandler(Action<IConsumer<byte[]?, byte[]?>, LogMessage> logHandler);

    /// <summary>
    ///     Builds the <see cref="IConsumer{TKey,TValue}" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IConsumer{TKey,TValue}" />.
    /// </returns>
    IConsumer<byte[]?, byte[]?> Build();
}
