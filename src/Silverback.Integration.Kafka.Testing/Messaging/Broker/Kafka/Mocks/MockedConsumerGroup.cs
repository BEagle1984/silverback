// Copyright (c) 2026 Sergio Aquilini
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

internal sealed class MockedConsumerGroup : IInternalMockedConsumerGroup, IDisposable
{
    private static readonly SimpleRebalanceStrategy SimpleRebalanceStrategy = new();

    private static readonly CooperativeStickyRebalanceStrategy CooperativeStickyRebalanceStrategy = new();

    // Assignment reads must remain possible while a rebalance holds the subscriptions semaphore
    private readonly System.Threading.Lock _stateLock = new();

    private readonly Dictionary<IMockedConfluentConsumer, PartitionAssignment> _partitionAssignments = [];

    private readonly List<ConsumerSubscription> _subscriptions = [];

    private readonly List<SubscribedConsumer> _subscribedConsumers = [];

    private readonly List<IMockedConfluentConsumer> _manuallyAssignedConsumers = [];

    private readonly ConcurrentDictionary<TopicPartition, TopicPartitionOffset> _committedOffsets = new();

    private readonly IInMemoryTopicCollection _topicCollection;

    private readonly SemaphoreSlim _subscriptionsChangeSemaphore = new(1, 1);

    private volatile bool _isRebalancing = true;

    private volatile bool _isRebalanceScheduled;

    // Invalidate drain checks that inspected an older group snapshot
    private long _stateVersion;

    public MockedConsumerGroup(string groupId, string bootstrapServers, IInMemoryTopicCollection topicCollection)
    {
        GroupId = Check.NotNull(groupId, nameof(groupId));
        BootstrapServers = Check.NotNull(bootstrapServers, nameof(bootstrapServers));
        _topicCollection = Check.NotNull(topicCollection, nameof(topicCollection));
    }

    public string GroupId { get; }

    public string BootstrapServers { get; }

    public IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets => _committedOffsets.Values.AsReadOnlyCollection();

    public bool IsRebalancing => _isRebalancing;

    public bool IsRebalanceScheduled => _isRebalanceScheduled;

    public void Subscribe(IMockedConfluentConsumer consumer, IEnumerable<string> topics)
    {
        try
        {
            _subscriptionsChangeSemaphore.Wait();

            lock (_stateLock)
            {
                UnsubscribeCore(consumer);

                foreach (string topic in topics)
                {
                    _subscriptions.Add(new ConsumerSubscription((MockedConfluentConsumer)consumer, topic));
                }

                _subscribedConsumers.Add(new SubscribedConsumer((MockedConfluentConsumer)consumer));

                _stateVersion++;
                Rebalance();
            }
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

            lock (_stateLock)
            {
                UnsubscribeCore(consumer);

                _stateVersion++;
                Rebalance();
            }
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

            lock (_stateLock)
            {
                UnassignCore(consumer);

                _partitionAssignments[consumer] = new ManualPartitionAssignment((MockedConfluentConsumer)consumer);

                foreach (TopicPartition topicPartition in partitions)
                {
                    _partitionAssignments[consumer].Partitions.Add(topicPartition);
                }

                _manuallyAssignedConsumers.Add(consumer);

                _stateVersion++;
                Rebalance();
            }
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

            lock (_stateLock)
            {
                UnassignCore(consumer);

                _stateVersion++;
                Rebalance();
            }
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

            lock (_stateLock)
            {
                if (_manuallyAssignedConsumers.Contains(consumer))
                    UnassignCore(consumer);

                if (_subscribedConsumers.Exists(subscribedConsumer => subscribedConsumer.Consumer == consumer))
                    UnsubscribeCore(consumer);

                _stateVersion++;
                Rebalance();
            }
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

    public void Rebalance()
    {
        lock (_stateLock)
        {
            if (_isRebalanceScheduled)
                return;

            _isRebalanceScheduled = true;
        }

        // Rebalance asynchronously to mimic the real Kafka
        Task.Run(async () =>
        {
            await Task.Delay(50).ConfigureAwait(false);
            await RebalanceAsync().ConfigureAwait(false);
        }).FireAndForget();
    }

    public IReadOnlyCollection<TopicPartition> GetAssignment(IMockedConfluentConsumer consumer)
    {
        lock (_stateLock)
        {
            return _partitionAssignments.TryGetValue(consumer, out PartitionAssignment? assignment)
                ? [.. assignment.Partitions]
                : [];
        }
    }

    public TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition) =>
        _committedOffsets.GetValueOrDefault(topicPartition);

    public long GetCommittedOffsetsCount(string topic) =>
        _committedOffsets.Values.Where(offset => offset.Topic == topic).Sum(offset => offset.Offset);

    public async ValueTask WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            MockedConfluentConsumer[] consumers;
            long stateVersion;

            lock (_stateLock)
            {
                consumers =
                [
                    .. _subscribedConsumers.Select(consumer => consumer.Consumer),
                    .. _manuallyAssignedConsumers.Cast<MockedConfluentConsumer>()
                ];
                stateVersion = _stateVersion;
            }

            if (consumers.All(consumer => HasFinishedConsuming(consumer, topicNames)))
            {
                lock (_stateLock)
                {
                    if (stateVersion == _stateVersion &&
                        (consumers.Length == 0 || !_isRebalancing && !_isRebalanceScheduled))
                    {
                        return;
                    }
                }
            }

            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public void NotifyAssignmentComplete(MockedConfluentConsumer consumer)
    {
        TaskCompletionSource<bool>? completion;

        lock (_stateLock)
        {
            completion = _subscribedConsumers.SingleOrDefault(subscribedConsumer => subscribedConsumer.Consumer == consumer)?
                .PartitionsAssignedTaskCompletionSource;
        }

        completion?.TrySetResult(true);
    }

    public void Dispose() => _subscriptionsChangeSemaphore.Dispose();

    private async Task RebalanceAsync()
    {
        try
        {
            await _subscriptionsChangeSemaphore.WaitAsync().ConfigureAwait(false);

            SubscribedConsumer[] consumers;

            lock (_stateLock)
            {
                _isRebalanceScheduled = false;
                _isRebalancing = true;
                _stateVersion++;

                if (_subscriptions.Count == 0)
                {
                    _isRebalancing = false;
                    return;
                }

                consumers = [.. _subscribedConsumers];
            }

            foreach (SubscribedConsumer consumer in consumers)
            {
                // Invalidate in-flight assignment without holding the group-state lock
                consumer.Consumer.OnRebalancing();
                TaskCompletionSource<bool> previousCompletion;

                lock (_stateLock)
                {
                    previousCompletion = consumer.PartitionsAssignedTaskCompletionSource;
                    consumer.PartitionsAssignedTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                previousCompletion.TrySetCanceled();
            }

            IReadOnlyList<TopicPartition> partitionsToAssign = GetPartitionsToAssign();
            RebalanceResult result;

            lock (_stateLock)
            {
                EnsurePartitionAssignmentsDictionaryIsInitialized();
                List<SubscriptionPartitionAssignment> subscriptionPartitionAssignments =
                    [.. _partitionAssignments.Values.OfType<SubscriptionPartitionAssignment>()];
                result = GetAssignmentStrategy() switch
                {
                    PartitionAssignmentStrategy.CooperativeSticky =>
                        CooperativeStickyRebalanceStrategy.Rebalance(partitionsToAssign, subscriptionPartitionAssignments),

                    // RoundRobin and Range aren't fully implemented, but both use eager revocation in the mock
                    _ => SimpleRebalanceStrategy.Rebalance(partitionsToAssign, subscriptionPartitionAssignments)
                };
            }

            InvokePartitionsRevokedCallbacks(result);

            // Give the MockedConfluentConsumers time to realize the partitions have been revoked and return from the Consume
            await Task.Delay(20).ConfigureAwait(false);

            _isRebalancing = false;

            await WaitUntilPartitionsAssignedAsync().ConfigureAwait(false);
        }
        finally
        {
            _subscriptionsChangeSemaphore.Release();
        }
    }

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
            .ForEach(consumer =>
            {
                _partitionAssignments[consumer] = new SubscriptionPartitionAssignment(consumer);
            });

    private PartitionAssignmentStrategy GetAssignmentStrategy()
    {
        if (_subscriptions.TrueForAll(subscription => subscription.Consumer.Config.PartitionAssignmentStrategy.HasValue &&
                                                      subscription.Consumer.Config.PartitionAssignmentStrategy.Value.HasFlag(PartitionAssignmentStrategy.CooperativeSticky)))
        {
            return PartitionAssignmentStrategy.CooperativeSticky;
        }

        if (_subscriptions.TrueForAll(subscription => subscription.Consumer.Config.PartitionAssignmentStrategy.HasValue &&
                                                      subscription.Consumer.Config.PartitionAssignmentStrategy.Value.HasFlag(PartitionAssignmentStrategy.RoundRobin)))
        {
            return PartitionAssignmentStrategy.RoundRobin;
        }

        return PartitionAssignmentStrategy.Range;
    }

    private List<TopicPartition> GetPartitionsToAssign() =>
    [
        .. _subscriptions.Select(subscription => subscription.Topic).Distinct()
            .Select(topicName => _topicCollection.Get(topicName, BootstrapServers))
            .SelectMany(topic =>
                topic.Partitions.Select(partition => new TopicPartition(topic.Name, partition.Partition)))
    ];

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
            _subscribedConsumers.Select(consumer =>
                Task.WhenAny(consumer.PartitionsAssignedTaskCompletionSource.Task, Task.Delay(100))));

    private bool HasFinishedConsuming(MockedConfluentConsumer consumer, IReadOnlyCollection<string> topicNames)
    {
        if (consumer.IsDisposed)
            return true;

        if (!consumer.TryGetAssignment(out IReadOnlyCollection<TopicPartition> assignment))
            return false;

        return assignment
            .Where(partition => topicNames.Count == 0 || topicNames.Contains(partition.Topic, StringComparer.Ordinal))
            .All(topicPartition =>
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

        public TaskCompletionSource<bool> PartitionsAssignedTaskCompletionSource { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
