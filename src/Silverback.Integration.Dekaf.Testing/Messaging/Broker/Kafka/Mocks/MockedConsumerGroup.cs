// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Admin;
using Dekaf.Testing;
using Silverback.Util;
using DekafTopicPartition = Dekaf.TopicPartition;
using IsolationLevel = Dekaf.Protocol.Messages.IsolationLevel;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     A view over a consumer group of the in-memory cluster, combining the offsets committed in the cluster with
///     the offsets stored by the consumers created by the <see cref="InMemoryDekafClientsFactory" />.
/// </summary>
internal sealed class MockedConsumerGroup : IMockedConsumerGroup
{
    private readonly InMemoryDekafClientsFactory _clientsFactory;

    // The last visible offset of each partition, cached along with the last stable offset it was read at: reading the
    // records clones all of them under the cluster lock, which would be repeated at each check while waiting
    private readonly ConcurrentDictionary<DekafTopicPartition, LastOffset> _lastOffsets = new();

    public MockedConsumerGroup(string groupId, InMemoryDekafClientsFactory clientsFactory)
    {
        GroupId = Check.NotNullOrEmpty(groupId, nameof(groupId));
        _clientsFactory = Check.NotNull(clientsFactory, nameof(clientsFactory));
    }

    public string GroupId { get; }

    public IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets =>
    [
        .. GetCommittedOffsets().Select(pair => new TopicPartitionOffset(pair.Key.Topic, pair.Key.Partition, pair.Value))
    ];

    private InMemoryKafkaCluster Cluster => _clientsFactory.Cluster;

    public TopicPartitionOffset? GetCommittedOffset(TopicPartition topicPartition)
    {
        Check.NotNull(topicPartition, nameof(topicPartition));

        return GetCommittedOffsets().TryGetValue(new DekafTopicPartition(topicPartition.Topic, topicPartition.Partition), out long offset)
            ? new TopicPartitionOffset(topicPartition, offset)
            : null;
    }

    public long GetCommittedOffsetsCount(string topic) =>
        GetCommittedOffsets().Where(pair => pair.Key.Topic == topic).Sum(pair => pair.Value);

    public ValueTask WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default) =>
        WaitUntilAllMessagesCoreAsync(topicNames, false, cancellationToken);

    public ValueTask WaitUntilAllMessagesAreCommittedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default) =>
        WaitUntilAllMessagesCoreAsync(topicNames, true, cancellationToken);

    private static bool IsRelevantTopic(string topic, IReadOnlyCollection<string> topicNames) =>
        topicNames.Count == 0 || topicNames.Contains(topic, StringComparer.Ordinal);

    private IReadOnlyDictionary<DekafTopicPartition, long> GetCommittedOffsets()
    {
        // The in-memory admin client completes synchronously, it only reads the cluster state
        InMemoryAdminClient adminClient = new(Cluster);

        try
        {
            return adminClient.ListConsumerGroupOffsetsAsync(GroupId).SafeWait();
        }
        finally
        {
            adminClient.DisposeAsync().SafeWait();
        }
    }

    private async ValueTask WaitUntilAllMessagesCoreAsync(IReadOnlyCollection<string> topicNames, bool waitForCommit, CancellationToken cancellationToken)
    {
        while (!HasFinishedConsuming(topicNames, waitForCommit))
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool HasFinishedConsuming(IReadOnlyCollection<string> topicNames, bool waitForCommit)
    {
        TrackingInMemoryConsumer[] consumers = [.. _clientsFactory.Consumers.Where(consumer => consumer.GroupId == GroupId && !consumer.IsClosed)];

        if (consumers.Length == 0)
            return true;

        if (consumers.Any(consumer => consumer.HasPendingAssignmentChange))
            return false;

        // A partition of a subscribed topic without owner means that a rebalance is still in progress
        HashSet<DekafTopicPartition> assigned = [.. consumers.SelectMany(consumer => consumer.Assignment)];

        if (consumers.SelectMany(consumer => consumer.Subscription).Distinct()
            .Where(topic => IsRelevantTopic(topic, topicNames))
            .SelectMany(GetTopicPartitions)
            .Any(partition => !assigned.Contains(partition)))
        {
            return false;
        }

        DekafTopicPartition[] relevantPartitions =
        [
            .. consumers
                .SelectMany(consumer => consumer.Assignment)
                .Where(partition => IsRelevantTopic(partition.Topic, topicNames))
                .Distinct()
        ];

        if (relevantPartitions.Length == 0)
            return true;

        // The in-memory admin client completes synchronously, it only reads the cluster state
        InMemoryAdminClient adminClient = new(Cluster);

        try
        {
            IReadOnlyDictionary<DekafTopicPartition, long> committedOffsets = waitForCommit
                ? adminClient.ListConsumerGroupOffsetsAsync(GroupId).SafeWait()
                : new Dictionary<DekafTopicPartition, long>();
            IReadOnlyDictionary<DekafTopicPartition, ListOffsetsResultInfo> lastStableOffsets = adminClient.ListOffsetsAsync(
                    relevantPartitions.Select(partition => new TopicPartitionOffsetSpec { TopicPartition = partition, Spec = OffsetSpec.Latest }),
                    new ListOffsetsOptions { IsolationLevel = IsolationLevel.ReadCommitted })
                .SafeWait();

            return consumers.All(consumer => consumer.Assignment
                .Where(partition => IsRelevantTopic(partition.Topic, topicNames))
                .All(partition =>
                {
                    long? lastOffset = GetLastOffset(partition, lastStableOffsets[partition].Offset);

                    if (lastOffset == null)
                        return true;

                    long? offset = waitForCommit
                        ? committedOffsets.TryGetValue(partition, out long committedOffset) ? committedOffset : null
                        : consumer.GetStoredOffset(partition);

                    return offset > lastOffset;
                }));
        }
        finally
        {
            adminClient.DisposeAsync().SafeWait();
        }
    }

    private IReadOnlyList<DekafTopicPartition> GetTopicPartitions(string topic) =>
        Cluster.ListTopics().Contains(topic, StringComparer.Ordinal) ? Cluster.GetTopicPartitions(topic) : [];

    // Only the records visible to a read_committed consumer are considered. The last stable offset can't be used
    // directly, since the transaction markers and the aborted records also take an offset.
    private long? GetLastOffset(DekafTopicPartition partition, long lastStableOffset)
    {
        if (_lastOffsets.TryGetValue(partition, out LastOffset? cached) && cached.LastStableOffset == lastStableOffset)
            return cached.Offset;

        long? lastOffset = Cluster.ReadRecords(partition.Topic, partition.Partition) is { Count: > 0 } records ? records[^1].Offset : null;
        _lastOffsets[partition] = new LastOffset(lastStableOffset, lastOffset);
        return lastOffset;
    }

    private sealed record LastOffset(long LastStableOffset, long? Offset);
}
