// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.KafkaConsumer
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class KafkaEventsSubscriber : ISubscriber
    {
        private readonly ILogger<KafkaEventsSubscriber> _logger;

        public KafkaEventsSubscriber(ILogger<KafkaEventsSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnPartitionsAssigned(KafkaPartitionsAssignedEvent message) =>
            _logger.LogInformation(
                "KafkaPartitionsAssignedEvent received: {count} partitions have been assigned ({partitions})",
                message.Partitions.Count,
                string.Join(", ", message.Partitions.Select(partition => partition.TopicPartition.ToString())));

        // public void OnPartitionsAssignedResetOffset(KafkaPartitionsAssignedEvent message)
        // {
        //     message.Partitions = message.Partitions
        //         .Select(topicPartitionOffset =>
        //             new TopicPartitionOffset(
        //                 topicPartitionOffset.TopicPartition,
        //                 Offset.Beginning))
        //         .ToList();
        // }
        
        public void OnPartitionsRevoked(KafkaPartitionsRevokedEvent message) =>
            _logger.LogInformation(
                "KafkaPartitionsRevokedEvent received: {count} partitions have been revoked ({partitions})",
                message.Partitions.Count,
                string.Join(", ", message.Partitions.Select(partition => partition.TopicPartition.ToString())));

        public void OnOffsetCommitted(KafkaOffsetsCommittedEvent message)
        {
            var committedOffsets = message.Offsets.Where(offset => offset.Offset != Offset.Unset).ToList();
            
            _logger.LogInformation(
                "KafkaOffsetsCommittedEvent received: {count} offsets have been committed ({offsets})",
                committedOffsets.Count,
                string.Join(", ", committedOffsets.Select(offset => offset.ToString())));
        }
    }
}