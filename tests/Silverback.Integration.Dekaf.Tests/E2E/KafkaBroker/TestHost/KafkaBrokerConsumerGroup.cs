// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Util;
using DekafTopicPartition = Dekaf.TopicPartition;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     A view over a consumer group of the test broker.
/// </summary>
/// <remarks>
///     The stored (not yet committed) offsets aren't observable on a real broker: waiting until the messages are
///     consumed is therefore the same as waiting until their offsets are committed. The auto-commit interval is
///     lowered by the <see cref="ContainerDekafClientsFactory" /> to keep this fast.
/// </remarks>
[SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Test code")]
internal sealed class KafkaBrokerConsumerGroup : IMockedConsumerGroup
{
    private static readonly TimeSpan SettlePeriod = TimeSpan.FromMilliseconds(500);

    private readonly KafkaBrokerInspector _inspector;

    private readonly IConsumerCollection _consumers;

    private readonly ILogger _logger;

    public KafkaBrokerConsumerGroup(string groupId, KafkaBrokerInspector inspector, IConsumerCollection consumers, ILogger logger)
    {
        _logger = logger;
        GroupId = Check.NotNullOrEmpty(groupId, nameof(groupId));
        _inspector = Check.NotNull(inspector, nameof(inspector));
        _consumers = Check.NotNull(consumers, nameof(consumers));
    }

    public string GroupId { get; }

    public IReadOnlyCollection<TopicPartitionOffset> CommittedOffsets =>
    [
        .. GetCommittedOffsets().Select(pair => new TopicPartitionOffset(pair.Key.Topic, pair.Key.Partition, pair.Value))
    ];

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
        WaitUntilAllMessagesAreCommittedAsync(topicNames, cancellationToken);

    public async ValueTask WaitUntilAllMessagesAreCommittedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default)
    {
        DateTime? finishedSince = null;

        // The transaction markers are written asynchronously after the producer's commit returns: until then the
        // committed records aren't visible and the partition looks fully consumed. The state must therefore hold
        // for a settle period before the wait completes.
        while (finishedSince == null || DateTime.UtcNow - finishedSince < SettlePeriod)
        {
            if (await HasFinishedConsumingAsync(topicNames, cancellationToken).ConfigureAwait(false))
                finishedSince ??= DateTime.UtcNow;
            else
                finishedSince = null;

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsRelevantTopic(string topic, IReadOnlyCollection<string> topicNames) =>
        topicNames.Count == 0 || topicNames.Contains(topic, StringComparer.Ordinal);

    private IReadOnlyDictionary<DekafTopicPartition, long> GetCommittedOffsets() => _inspector.GetCommittedOffsetsAsync(GroupId).SafeWait();

    private async Task<bool> HasFinishedConsumingAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken)
    {
        KafkaConsumer[] consumers =
        [
            .. _consumers.OfType<KafkaConsumer>()
                .Where(consumer => consumer.Configuration.GroupId == GroupId && consumer.Client.Status == ClientStatus.Initialized)
        ];

        if (consumers.Length == 0)
        {
            _logger.LogTrace("Group {GroupId}: no connected consumer", GroupId);
            return true;
        }

        // The subscribed topics are consumed entirely, while a static assignment only covers the configured partitions
        string[] topics =
        [
            .. consumers.Where(consumer => !consumer.Configuration.IsStaticAssignment)
                .SelectMany(consumer => consumer.Configuration.Endpoints)
                .SelectMany(endpoint => endpoint.TopicPartitions)
                .Select(topicPartition => topicPartition.Topic)
                .Where(topic => IsRelevantTopic(topic, topicNames))
                .Distinct(StringComparer.Ordinal)
        ];

        IReadOnlyList<DekafTopicPartition> subscribedPartitions = await _inspector.GetTopicPartitionsAsync(topics, cancellationToken).ConfigureAwait(false);

        // The topics may not be created yet
        if (subscribedPartitions.Count == 0 && topics.Length > 0)
            return false;

        DekafTopicPartition[] partitions =
        [
            .. subscribedPartitions.Union(
                consumers.Where(consumer => consumer.Configuration.IsStaticAssignment)
                    .SelectMany(consumer => consumer.Configuration.Endpoints)
                    .SelectMany(endpoint => endpoint.TopicPartitions)
                    .Where(topicPartition => IsRelevantTopic(topicPartition.Topic, topicNames))
                    .Select(topicPartition => new DekafTopicPartition(topicPartition.Topic, topicPartition.Partition)))
        ];

        // A partition without owner means that the group is still rebalancing
        HashSet<DekafTopicPartition> assigned =
        [
            .. consumers.SelectMany(consumer => consumer.Client.Assignment)
                .Select(topicPartition => new DekafTopicPartition(topicPartition.Topic, topicPartition.Partition))
        ];

        if (partitions.Any(partition => !assigned.Contains(partition)))
        {
            _logger.LogTrace("Group {GroupId}: rebalancing ({Assigned}/{Partitions} partitions assigned)", GroupId, assigned.Count, partitions.Length);
            return false;
        }

        IReadOnlyDictionary<DekafTopicPartition, long> committedOffsets = await _inspector.GetCommittedOffsetsAsync(GroupId, cancellationToken).ConfigureAwait(false);

        foreach (DekafTopicPartition partition in partitions)
        {
            long committedOffset = committedOffsets.GetValueOrDefault(partition);

            if (await _inspector.HasVisibleRecordsAsync(partition, committedOffset, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogTrace("Group {GroupId}: {Partition} has pending records after offset {Offset}", GroupId, partition, committedOffset);
                return false;
            }
        }

        _logger.LogTrace("Group {GroupId}: all {Partitions} partitions consumed ({Offsets})", GroupId, partitions.Length, string.Join(", ", committedOffsets));
        return true;
    }
}
