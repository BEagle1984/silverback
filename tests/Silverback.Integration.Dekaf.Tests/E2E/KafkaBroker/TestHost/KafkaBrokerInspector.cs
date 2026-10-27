// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Admin;
using Dekaf.Consumer;
using Dekaf.Errors;
using Silverback.Util;
using DekafKafka = Dekaf.Kafka;
using DekafTopicPartition = Dekaf.TopicPartition;
using DekafTopicPartitionOffset = Dekaf.TopicPartitionOffset;
using IsolationLevel = Dekaf.Protocol.Messages.IsolationLevel;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     Queries the state of the test broker: topic partitions, committed offsets and whether a partition still holds
///     records that a read_committed consumer would receive.
/// </summary>
internal sealed class KafkaBrokerInspector : IAsyncDisposable
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(200);

    private readonly IAdminClient _adminClient;

    private readonly IKafkaConsumer<byte[], byte[]> _probeConsumer;

    // The probe consumer API must not be called concurrently
    private readonly SemaphoreSlim _probeSemaphore = new(1, 1);

    public KafkaBrokerInspector(KafkaContainerHost containerHost)
    {
        Check.NotNull(containerHost, nameof(containerHost));

        _adminClient = DekafKafka.CreateAdminClient().WithBootstrapServers(containerHost.BootstrapServers).Build();
        _probeConsumer = DekafKafka.CreateConsumer<byte[], byte[]>()
            .WithBootstrapServers(containerHost.BootstrapServers)
            .WithIsolationLevel(IsolationLevel.ReadCommitted)
            .WithAutoOffsetReset(AutoOffsetReset.Earliest)
            .Build();
    }

    public async Task<IReadOnlyDictionary<DekafTopicPartition, long>> GetCommittedOffsetsAsync(string groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _adminClient.ListConsumerGroupOffsetsAsync(groupId, cancellationToken).ConfigureAwait(false);
        }
        catch (KafkaException)
        {
            // The group doesn't exist until its first member joins
            return new Dictionary<DekafTopicPartition, long>();
        }
    }

    public async Task<IReadOnlyList<DekafTopicPartition>> GetTopicPartitionsAsync(IReadOnlyCollection<string> topics, CancellationToken cancellationToken)
    {
        if (topics.Count == 0)
            return [];

        try
        {
            IReadOnlyDictionary<string, TopicDescription> descriptions =
                await _adminClient.DescribeTopicsAsync(topics, cancellationToken).ConfigureAwait(false);

            return
            [
                .. descriptions.Values.SelectMany(description =>
                    description.Partitions.Select(partition => new DekafTopicPartition(description.Name, partition.PartitionIndex)))
            ];
        }
        catch (KafkaException ex)
        {
            throw new InvalidOperationException($"Failed to describe the topics {string.Join(", ", topics)}.", ex);
        }
    }

    // A committed offset below the last stable offset doesn't necessarily mean that messages are pending: the gap
    // may only contain transaction markers or aborted records, which are never delivered to the consumers
    public async Task<bool> HasVisibleRecordsAsync(DekafTopicPartition partition, long fromOffset, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<DekafTopicPartition, ListOffsetsResultInfo> lastStableOffsets = await _adminClient.ListOffsetsAsync(
                [new TopicPartitionOffsetSpec { TopicPartition = partition, Spec = OffsetSpec.Latest }],
                new ListOffsetsOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                cancellationToken)
            .ConfigureAwait(false);

        if (!lastStableOffsets.TryGetValue(partition, out ListOffsetsResultInfo? lastStableOffset) || fromOffset >= lastStableOffset.Offset)
            return false;

        await _probeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _probeConsumer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            _probeConsumer.Partitions.Assign(partition);
            _probeConsumer.Positions.Seek(new DekafTopicPartitionOffset(partition.Topic, partition.Partition, fromOffset));

            // Read until a visible record is found or the position moves past the markers up to the last stable offset
            while (true)
            {
                ConsumeResult<byte[], byte[]>? result = await _probeConsumer.ConsumeOneAsync(ProbeTimeout, cancellationToken).ConfigureAwait(false);

                if (result is { IsPartitionEof: false } record && record.Offset < lastStableOffset.Offset)
                    return true;

                if (_probeConsumer.Positions.GetPosition(partition) >= lastStableOffset.Offset)
                    return false;
            }
        }
        finally
        {
            _probeSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _probeConsumer.DisposeAsync().ConfigureAwait(false);
        await _adminClient.DisposeAsync().ConfigureAwait(false);
        _probeSemaphore.Dispose();
    }
}
