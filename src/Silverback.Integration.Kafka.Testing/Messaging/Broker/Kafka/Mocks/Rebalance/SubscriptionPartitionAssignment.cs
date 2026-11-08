// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;

internal class SubscriptionPartitionAssignment : PartitionAssignment
{
    public SubscriptionPartitionAssignment(IMockedConfluentConsumer consumer)
        : base(consumer)
    {
    }

    public bool IsSubscribedTo(string topic) =>
        Consumer.Subscription.Exists(subscription =>
            KafkaTopicPattern.IsMatch(subscription, topic, KafkaTopicPattern.IsFullMatchRequired(Consumer.Config.GroupProtocol)));
}
