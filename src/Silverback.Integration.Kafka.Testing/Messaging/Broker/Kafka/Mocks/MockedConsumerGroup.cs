// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks.Rebalance;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class MockedConsumerGroup : IMockedConsumerGroup, IDisposable
{
    private static readonly SimpleRebalanceStrategy SimpleRebalanceStrategy = new();

    private static readonly CooperativeStickyRebalanceStrategy CooperativeStickyRebalanceStrategy = new();

    private readonly Dictionary<IMockedConfluentConsumer, PartitionAssignment> _partitionAssignments = [];

    private readonly List<ConsumerSubscription> _subscriptions = [];

    private readonly List<SubscribedConsumer> _subscribedConsumers = [];

    private readonly List<IMockedConfluentConsumer> _manuallyAssignedConsumers = [];

    private readonly ConcurrentDictionary<TopicPartition, TopicPartitionOffset> _committedOffsets = new();

    private readonly IInMemoryTopicCollection _topicCollection;

    private readonly SemaphoreSlim _subscriptionsChangeSemaphore = new(1, 1);

    public MockedConsumerGroup(string groupId, string bootstrapServers, IInMemoryTopicCollection topicCollection)
    {
        GroupId = Check.NotNull(groupId, nameof(groupId));
        BootstrapServers = Check.NotNull(bootstrapServers, nameof(bootstrapServers));
        _topicCollection = Check.NotNull(topicCollection, nameof(topicCollection));
    }

    public string GroupId { get; }

    public string BootstrapServers { get; }

    public IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets => _committedOffsets.Values.AsReadOnlyCollection();

    public bool IsRebalancing { get; private set; } = true;

    public bool IsRebalanceScheduled { get; private set; }

    public void Subscribe(IMockedConfluentConsumer consumer, IEnumerable<string> topics)
    {
        try
        {
            _subscriptionsChangeSemaphore.Wait();

            UnsubscribeCore(consumer);

            foreach (string topic in topics)
            {
                _subscriptions.Add(new ConsumerSubscription((MockedConfluentConsumer)consumer, topic));
            }

            _subscribedConsumers.Add(new SubscribedConsumer((MockedConfluentConsumer)consumer));

            ScheduleRebalance();
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

    public void Unsubscribe(IMockedConfluentConsumer consumer)
    {
        try
        {
            _subscriptionsChangeSemaphore.Wait();

            UnsubscribeCore(consumer);

            ScheduleRebalance();
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

    public void Assign(IMockedConfluentConsumer consumer, IEnumerable<TopicPartition> partitions)
    {
        try
        {
            _subscriptionsChangeSemaphore.Wait();

            UnassignCore(consumer);

            _partitionAssignments[consumer] = new ManualPartitionAssignment((MockedConfluentConsumer)consumer);

            foreach (TopicPartition topicPartition in partitions)
            {
                _partitionAssignments[consumer].Partitions.Add(topicPartition);
            }

            _manuallyAssignedConsumers.Add(consumer);

            ScheduleRebalance();
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

    public void Unassign(IMockedConfluentConsumer consumer)
    {
        try
        {
            _subscriptionsChangeSemaphore.Wait();

            UnassignCore(consumer);

            ScheduleRebalance();
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

    public void Remove(IMockedConfluentConsumer consumer)
    {
        try
        {
            _subscriptionsChangeSemaphore.Wait();

            if (_manuallyAssignedConsumers.Contains(consumer))
                UnassignCore(consumer);

            if (_subscribedConsumers.Any(subscribedConsumer => subscribedConsumer.Consumer == consumer))
                UnsubscribeCore(consumer);

            ScheduleRebalance();
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

    public void Commit(IEnumerable<TopicPartitionOffset> offsets)
    {
        foreach (TopicPartitionOffset offset in offsets)
        {
            _committedOffsets.AddOrUpdate(
                offset.TopicPartition,
                _ => offset,
                (_, _) => offset);
        }
    }

    public void ScheduleRebalance()
    {
        if (IsRebalanceScheduled)
            return;

        IsRebalanceScheduled = true;

        // Rebalance asynchronously to mimic the real Kafka
        Task.Run(
            async () =>
            {
                await Task.Delay(50).ConfigureAwait(false);
                await RebalanceAsync().ConfigureAwait(false);
            }).FireAndForget();
    }

    public async Task<RebalanceResult> RebalanceAsync()
    {
        try
        {
            await _subscriptionsChangeSemaphore.WaitAsync().ConfigureAwait(false);

            IsRebalanceScheduled = false;
            IsRebalancing = true;

            if (_subscriptions.Count == 0)
                return RebalanceResult.Empty;

            _subscribedConsumers.ForEach(
                consumer =>
                {
                    consumer.PartitionsAssignedTaskCompletionSource.TrySetCanceled();
                    consumer.PartitionsAssignedTaskCompletionSource = new TaskCompletionSource<bool>();
                    consumer.Consumer.OnRebalancing();
                });

            EnsurePartitionAssignmentsDictionaryIsInitialized();
            IReadOnlyList<TopicPartition> partitionsToAssign = GetPartitionsToAssign();
            List<SubscriptionPartitionAssignment> subscriptionPartitionAssignments =
                _partitionAssignments.Values.OfType<SubscriptionPartitionAssignment>().ToList();
            RebalanceResult result = GetAssignmentStrategy() switch
            {
                PartitionAssignmentStrategy.CooperativeSticky =>
                    CooperativeStickyRebalanceStrategy.Rebalance(partitionsToAssign, subscriptionPartitionAssignments),

                // RoundRobin and Range strategies aren't properly implemented but it shouldn't make any difference for the in-memory tests
                _ => SimpleRebalanceStrategy.Rebalance(partitionsToAssign, subscriptionPartitionAssignments)
            };
            InvokePartitionsRevokedCallbacks(result);

            IsRebalancing = false;

            await WaitUntilPartitionsAssignedAsync().ConfigureAwait(false);

            // Add a delay to avoid deadlocks
            // TODO: Investigate why it's needed
            await Task.Delay(50).ConfigureAwait(false);

            return result;
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

    public IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer) =>
        _partitionAssignments[consumer].Partitions;

    public TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition) =>
        _committedOffsets.TryGetValue(topicPartition, out TopicPartitionOffset? offset) ? offset : null;

    public long GetCommittedOffsetsCount(string topic) =>
        _committedOffsets.Values.Where(offset => offset.Topic == topic).Sum(offset => offset.Offset);

    public async Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_subscribedConsumers.Select(subscribedConsumer => subscribedConsumer.Consumer).All(HasFinishedConsuming) &&
                _manuallyAssignedConsumers.All(HasFinishedConsuming))
            {
                return;
            }

            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public void NotifyAssignmentComplete(MockedConfluentConsumer consumer) =>
        _subscribedConsumers.SingleOrDefault(subscribedConsumer => subscribedConsumer.Consumer == consumer)?
            .PartitionsAssignedTaskCompletionSource.TrySetResult(true);

    public void Dispose() => _subscriptionsChangeSemaphore.Dispose();

    private void UnsubscribeCore(IMockedConfluentConsumer consumer)
    {
        _partitionAssignments.Remove(consumer);
        _subscriptions.RemoveAll(subscription => subscription.Consumer == consumer);

        SubscribedConsumer? subscribedConsumer =
            _subscribedConsumers.SingleOrDefault(subscribedConsumer => subscribedConsumer.Consumer == consumer);

        if (subscribedConsumer != null)
        {
            subscribedConsumer.PartitionsAssignedTaskCompletionSource.TrySetCanceled();
            _subscribedConsumers.Remove(subscribedConsumer);
        }
    }

    private void UnassignCore(IMockedConfluentConsumer consumer)
    {
        _partitionAssignments.Remove(consumer);
        _manuallyAssignedConsumers.Remove(consumer);
    }

    private void EnsurePartitionAssignmentsDictionaryIsInitialized() =>
        _subscriptions
            .Select(subscription => subscription.Consumer)
            .Where(consumer => !_partitionAssignments.ContainsKey(consumer))
            .ForEach(
                consumer =>
                {
                    _partitionAssignments[consumer] = new SubscriptionPartitionAssignment(consumer);
                });

    private PartitionAssignmentStrategy GetAssignmentStrategy()
    {
        if (_subscriptions.All(
            subscription => subscription.Consumer.Config.PartitionAssignmentStrategy.HasValue &&
                            subscription.Consumer.Config.PartitionAssignmentStrategy.Value.HasFlag(PartitionAssignmentStrategy.CooperativeSticky)))
        {
            return PartitionAssignmentStrategy.CooperativeSticky;
        }

        if (_subscriptions.All(
            subscription => subscription.Consumer.Config.PartitionAssignmentStrategy.HasValue &&
                            subscription.Consumer.Config.PartitionAssignmentStrategy.Value.HasFlag(PartitionAssignmentStrategy.RoundRobin)))
        {
            return PartitionAssignmentStrategy.RoundRobin;
        }

        return PartitionAssignmentStrategy.Range;
    }

    private List<TopicPartition> GetPartitionsToAssign() =>
        _subscriptions.Select(subscription => subscription.Topic).Distinct()
            .Select(topicName => _topicCollection.Get(topicName, BootstrapServers))
            .SelectMany(
                topic =>
                    topic.Partitions.Select(partition => new TopicPartition(topic.Name, partition.Partition)))
            .ToList();

    private void InvokePartitionsRevokedCallbacks(RebalanceResult result)
    {
        foreach (MockedConfluentConsumer consumer in _subscribedConsumers.Select(subscribedConsumer => subscribedConsumer.Consumer))
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

    private Task<Task[]> WaitUntilPartitionsAssignedAsync() =>
        Task.WhenAll(
            _subscribedConsumers.Select(
                consumer =>
                    Task.WhenAny(consumer.PartitionsAssignedTaskCompletionSource.Task, Task.Delay(1000))));

    private bool HasFinishedConsuming(IMockedConfluentConsumer consumer)
    {
        if (consumer.IsDisposed)
            return true;

        if (!consumer.PartitionsAssigned)
            return false;

        return consumer.Assignment.All(
            topicPartition =>
            {
                IInMemoryTopic topic = _topicCollection.Get(topicPartition.Topic, consumer.Config);
                Offset lastOffset = topic.Partitions[topicPartition.Partition].LastOffset;

                if (lastOffset < 0)
                    return true;

                if (string.IsNullOrEmpty(consumer.Config.GroupId))
                    return consumer.GetStoredOffset(topicPartition).Offset > lastOffset;

                return _committedOffsets.TryGetValue(topicPartition, out TopicPartitionOffset? committedOffset) &&
                       committedOffset.Offset > lastOffset;
            });
    }

    private sealed class SubscribedConsumer
    {
        public SubscribedConsumer(MockedConfluentConsumer consumer)
        {
            Consumer = consumer;
        }

        public MockedConfluentConsumer Consumer { get; }

        public TaskCompletionSource<bool> PartitionsAssignedTaskCompletionSource { get; set; } = new();
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
