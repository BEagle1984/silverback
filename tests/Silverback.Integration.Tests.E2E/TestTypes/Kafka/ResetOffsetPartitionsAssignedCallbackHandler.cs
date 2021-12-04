// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Tests.Integration.E2E.TestTypes.Kafka;

public class ResetOffsetPartitionsAssignedCallbackHandler : IKafkaPartitionsAssignedCallback
{
    public IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
        IReadOnlyCollection<TopicPartition> topicPartitions,
        KafkaConsumer consumer) =>
        topicPartitions.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Beginning));
}
