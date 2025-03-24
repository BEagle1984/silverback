// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;

internal class ManualPartitionAssignment : PartitionAssignment
{
    public ManualPartitionAssignment(IMockedConfluentConsumer consumer)
        : base(consumer)
    {
    }
}
