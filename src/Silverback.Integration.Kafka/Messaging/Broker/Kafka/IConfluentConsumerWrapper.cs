// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the underlying <see cref="Consumer{TKey,TValue}" /> and handles the connection lifecycle.
/// </summary>
public interface IConfluentConsumerWrapper : IBrokerClient
{
    /// <summary>
    ///     Gets the consumer configuration.
    /// </summary>
    KafkaConsumerConfiguration Configuration { get; }

    /// <summary>
    ///     Gets the list of partitions currently assigned to this consumer.
    /// </summary>
    IReadOnlyList<TopicPartition> Assignment { get; }

    /// <summary>
    ///     Gets or sets the related consumer instance.
    /// </summary>
    KafkaConsumer Consumer { get; set; }

    /// <summary>
    ///     Poll for new messages (or events). This call blocks until a <see cref="ConsumeResult{TKey,TValue}" /> is available or the
    ///     operation has been cancelled.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     The next <see cref="ConsumeResult{TKey,TValue}" />.
    /// </returns>
    ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken);

    /// <summary>
    ///     Stores the specified offset for the specified partition. <br />
    ///     The offset will be committed (written) to the offset store according to <see cref="KafkaConsumerConfiguration.AutoCommitIntervalMs" />
    ///     or with a call to the <see cref="Commit" /> method.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset to be stored.
    /// </param>
    void StoreOffset(TopicPartitionOffset topicPartitionOffset);

    /// <summary>
    ///     Commits all stored offsets.
    /// </summary>
    void Commit();

    /// <summary>
    ///     Pauses the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    void Pause(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Resumes the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    void Resume(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Seeks the specified partition to the specified offset.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset.
    /// </param>
    void Seek(TopicPartitionOffset topicPartitionOffset);
}
