// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;

internal class SubscriptionPartitionAssignment : PartitionAssignment
{
    public SubscriptionPartitionAssignment(IMockedConfluentConsumer consumer)
        : base(consumer)
    {
    }
}
