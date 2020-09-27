// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Topics
{
    public interface IInMemoryTopic
    {
        Offset Push(int partition, Message<byte[], byte[]> message);

        ConsumeResult<byte[]?, byte[]?> Pull(
            string groupId,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            CancellationToken cancellationToken);

        void Subscribe(MockedKafkaConsumer consumer);

        void Unsubscribe(MockedKafkaConsumer consumer);

        void Commit(string groupId, IEnumerable<TopicPartitionOffset> partitionOffsets);

        Offset GetCommittedOffset(Partition partition, string groupId);

        IReadOnlyCollection<TopicPartitionOffset> GetCommittedOffsets(string groupId);

        long GetCommittedOffsetsCount(string groupId);

        Offset GetLastOffset(Partition partition);

        /// <summary>
        ///     Returns a <see cref="Task"/> that completes when all messages routed to the consumers have been processed.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumed(TimeSpan? timeout = null);
    }
}
