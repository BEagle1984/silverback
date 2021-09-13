// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance
{
    internal abstract class PartitionAssignment
    {
        protected PartitionAssignment(IMockedConfluentConsumer consumer)
        {
            Consumer = consumer;
        }

        public IMockedConfluentConsumer Consumer { get; }

        public List<TopicPartition> Partitions { get; } = new();
    }
}
