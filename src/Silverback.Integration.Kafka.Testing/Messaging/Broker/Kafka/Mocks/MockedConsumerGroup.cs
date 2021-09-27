// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal class MockedConsumerGroup : IMockedConsumerGroup
    {
        private static readonly SimpleRebalanceStrategy SimpleRebalanceStrategy = new();

        private static readonly CooperativeStickyRebalanceStrategy CooperativeStickyRebalanceStrategy = new();

        private readonly Dictionary<IMockedConfluentConsumer, PartitionAssignment> _partitionAssignments =
            new();

        private readonly List<ConsumerSubscription> _subscriptions = new();

        private readonly List<MockedConfluentConsumer> _subscribedConsumers = new();

        private readonly List<IMockedConfluentConsumer> _manuallyAssignedConsumers = new();

        private readonly ConcurrentDictionary<TopicPartition, TopicPartitionOffset> _committedOffsets = new();

        private readonly IInMemoryTopicCollection _topicCollection;

        private bool _rebalanceScheduled;

        public MockedConsumerGroup(
            string groupId,
            string bootstrapServers,
            IInMemoryTopicCollection topicCollection)
        {
            GroupId = groupId;
            BootstrapServers = bootstrapServers;
            _topicCollection = topicCollection;
        }

        public string GroupId { get; }

        public string BootstrapServers { get; }

        public IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets =>
            _committedOffsets.Values.AsReadOnlyCollection();

        public bool IsRebalancing { get; set; } = true;

        public void Subscribe(IMockedConfluentConsumer consumer, IEnumerable<string> topics)
        {
            lock (_partitionAssignments)
            {
                Unsubscribe(consumer);

                var mockedConsumer = (MockedConfluentConsumer)consumer;

                foreach (string topic in topics)
                {
                    _subscriptions.Add(new ConsumerSubscription(mockedConsumer, topic));
                }

                _subscribedConsumers.Add(mockedConsumer);

                ScheduleRebalance();
            }
        }

        public void Unsubscribe(IMockedConfluentConsumer consumer)
        {
            lock (_partitionAssignments)
            {
                _partitionAssignments.Remove(consumer);
                _subscriptions.RemoveAll(subscription => subscription.Consumer == consumer);
                _subscribedConsumers.Remove((MockedConfluentConsumer)consumer);

                ScheduleRebalance();
            }
        }

        public void Assign(IMockedConfluentConsumer consumer, IEnumerable<TopicPartition> partitions)
        {
            lock (_partitionAssignments)
            {
                Unassign(consumer);

                _partitionAssignments[consumer] =
                    new ManualPartitionAssignment((MockedConfluentConsumer)consumer);

                foreach (var topicPartition in partitions)
                {
                    _partitionAssignments[consumer].Partitions.Add(topicPartition);
                }

                _manuallyAssignedConsumers.Add(consumer);

                ScheduleRebalance();
            }
        }

        public void Unassign(IMockedConfluentConsumer consumer)
        {
            _partitionAssignments.Remove(consumer);
            _manuallyAssignedConsumers.Remove(consumer);

            ScheduleRebalance();
        }

        public void Remove(IMockedConfluentConsumer consumer)
        {
            if (_manuallyAssignedConsumers.Contains(consumer))
                Unassign(consumer);

            if (_subscribedConsumers.Contains(consumer))
                Unsubscribe(consumer);
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
            foreach (var offset in offsets)
            {
                _committedOffsets.AddOrUpdate(
                    offset.TopicPartition,
                    _ => offset,
                    (_, _) => offset);
            }
        }

        public RebalanceResult Rebalance()
        {
            lock (_partitionAssignments)
            {
                _rebalanceScheduled = false;
                IsRebalancing = true;

                if (_subscriptions.Count == 0)
                    return RebalanceResult.Empty;

                _subscribedConsumers.ForEach(consumer => consumer.OnRebalancing());

                EnsurePartitionAssignmentsDictionaryIsInitialized();

                RebalanceResult result;

                IReadOnlyList<TopicPartition> partitionsToAssign = GetPartitionsToAssign();
                List<SubscriptionPartitionAssignment> subscriptionPartitionAssignments =
                    _partitionAssignments.Values.OfType<SubscriptionPartitionAssignment>().ToList();

                switch (GetAssignmentStrategy())
                {
                    case PartitionAssignmentStrategy.CooperativeSticky:
                        result = CooperativeStickyRebalanceStrategy.Rebalance(
                            partitionsToAssign,
                            subscriptionPartitionAssignments);
                        break;

                    // RoundRobin and Range strategies aren't properly implemented but it shouldn't make any
                    // difference for the in-memory tests
                    default:
                        result = SimpleRebalanceStrategy.Rebalance(
                            partitionsToAssign,
                            subscriptionPartitionAssignments);
                        break;
                }

                InvokePartitionsRevokedCallbacks(result);

                IsRebalancing = false;

                return result;
            }
        }

        public IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer) =>
            _partitionAssignments[consumer].Partitions;

        public TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition) =>
            _committedOffsets.TryGetValue(topicPartition, out var offset) ? offset : null;

        public long GetCommittedOffsetsCount(string topic) =>
            _committedOffsets.Values.Where(offset => offset.Topic == topic).Sum(offset => offset.Offset);

        public async Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_subscribedConsumers.All(HasFinishedConsuming) &&
                    _manuallyAssignedConsumers.All(HasFinishedConsuming))
                {
                    return;
                }

                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        private void ScheduleRebalance()
        {
            if (_rebalanceScheduled)
                return;

            // Rebalance asynchronously to mimic the real Kafka
            Task.Run(Rebalance).FireAndForget();

            _rebalanceScheduled = true;
        }

        private void EnsurePartitionAssignmentsDictionaryIsInitialized() =>
            _subscriptions
                .Select(subscription => subscription.Consumer)
                .Where(consumer => !_partitionAssignments.ContainsKey(consumer))
                .ForEach(consumer =>
                {
                    _partitionAssignments[consumer] = new SubscriptionPartitionAssignment(consumer);
                });

        private PartitionAssignmentStrategy GetAssignmentStrategy()
        {
            if (_subscriptions.All(
                subscription => subscription.Consumer.Config.PartitionAssignmentStrategy.HasValue &&
                                subscription.Consumer.Config.PartitionAssignmentStrategy.Value.HasFlag(
                                    PartitionAssignmentStrategy.CooperativeSticky)))
            {
                return PartitionAssignmentStrategy.CooperativeSticky;
            }

            if (_subscriptions.All(
                subscription => subscription.Consumer.Config.PartitionAssignmentStrategy.HasValue &&
                                subscription.Consumer.Config.PartitionAssignmentStrategy.Value.HasFlag(
                                    PartitionAssignmentStrategy.RoundRobin)))
            {
                return PartitionAssignmentStrategy.RoundRobin;
            }

            return PartitionAssignmentStrategy.Range;
        }

        private IReadOnlyList<TopicPartition> GetPartitionsToAssign() =>
            _subscriptions.Select(subscription => subscription.Topic).Distinct()
                .Select(topicName => _topicCollection.Get(topicName, BootstrapServers))
                .SelectMany(
                    topic =>
                        topic.Partitions.Select(partition => new TopicPartition(topic.Name, partition.Partition)))
                .ToList();

        private void InvokePartitionsRevokedCallbacks(RebalanceResult result)
        {
            foreach (var consumer in _subscribedConsumers)
            {
                if (result.RevokedPartitions.TryGetValue(
                    consumer,
                    out IReadOnlyCollection<TopicPartition>? revokedPartitions) &&
                    revokedPartitions.Count > 0)
                {
                    consumer.OnPartitionsRevoked(revokedPartitions);
                }
            }
        }

        private bool HasFinishedConsuming(IMockedConfluentConsumer consumer)
        {
            if (consumer.Disposed)
                return true;

            if (!consumer.PartitionsAssigned)
                return false;

            return consumer.Assignment.All(
                topicPartition =>
                {
                    var topic = _topicCollection.Get(topicPartition.Topic, consumer.Config);
                    var lastOffset = topic.Partitions[topicPartition.Partition].LastOffset;

                    if (lastOffset < 0)
                        return true;

                    if (string.IsNullOrEmpty(consumer.Config.GroupId))
                        return consumer.GetStoredOffset(topicPartition).Offset > lastOffset;

                    return _committedOffsets.TryGetValue(topicPartition, out var committedOffset) &&
                           committedOffset.Offset > lastOffset;
                });
        }

        private sealed class ConsumerSubscription
        {
            public ConsumerSubscription(MockedConfluentConsumer consumer, string topic)
            {
                Consumer = consumer;
                Topic = topic;
            }

            public MockedConfluentConsumer Consumer { get; }

            public string Topic { get; }
        }
    }
}
