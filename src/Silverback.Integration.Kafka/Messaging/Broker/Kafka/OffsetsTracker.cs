// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Keeps track of the offsets of the messages being produced or consumed.
/// </summary>
public sealed class OffsetsTracker : IBrokerMessageIdentifiersTracker
{
    private readonly ConcurrentDictionary<TopicPartition, KafkaOffset> _commitOffsets = new();

    private readonly ConcurrentDictionary<TopicPartition, KafkaOffset> _rollbackOffsets = new();

    /// <summary>
    ///     Tracks the specified offset.
    /// </summary>
    /// <param name="offset">
    ///     The offset to be tracked.
    /// </param>
    public void TrackOffset(KafkaOffset offset)
    {
        Check.NotNull(offset, nameof(offset));

        _commitOffsets.AddOrUpdate(
            offset.TopicPartition,
            static (_, newOffset) => newOffset,
            static (_, existingOffset, newOffset) => newOffset > existingOffset ? newOffset : existingOffset,
            offset);

        _rollbackOffsets.AddOrUpdate(
            offset.TopicPartition,
            static (_, newOffset) => newOffset,
            static (_, existingOffset, newOffset) => existingOffset.Offset == Offset.Unset ? newOffset : existingOffset,
            offset);
    }

    /// <summary>
    ///     Tracks the specified offset.
    /// </summary>
    /// <param name="offset">
    ///     The offset to be tracked.
    /// </param>
    public void TrackOffset(TopicPartitionOffset offset) => TrackOffset(new KafkaOffset(offset));

    /// <summary>
    ///     Removes the tracked offset for the specified partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The partition to be untracked.
    /// </param>
    public void UntrackPartition(TopicPartition topicPartition)
    {
        _rollbackOffsets.TryRemove(topicPartition, out _);
        _commitOffsets.TryRemove(topicPartition, out _);
    }

    /// <summary>
    ///     Marks the specified offset as committed.
    /// </summary>
    /// <param name="offset">
    ///     The offset to be marked as committed.
    /// </param>
    public void Commit(KafkaOffset offset)
    {
        Check.NotNull(offset, nameof(offset));

        _rollbackOffsets.AddOrUpdate(
            offset.TopicPartition,
            static (_, newOffset) => newOffset,
            static (_, existingOffset, newOffset) => newOffset > existingOffset ? newOffset : existingOffset,
            new KafkaOffset(offset.TopicPartition, offset.Offset + 1)); // Commit next offset (+1)
    }

    /// <summary>
    ///     Gets the offsets to be used to commit after successful processing.
    /// </summary>
    /// <returns>
    ///     The offsets to be used to commit.
    /// </returns>
    public IReadOnlyCollection<KafkaOffset> GetCommitOffsets() => _commitOffsets.Values.AsReadOnlyCollection();

    /// <summary>
    ///     Gets the offsets to be used to rollback in case of error.
    /// </summary>
    /// <returns>
    ///     The offsets to be used to rollback.
    /// </returns>
    public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackOffSets() => _rollbackOffsets.Values.AsReadOnlyCollection();

    /// <inheritdoc cref="IBrokerMessageIdentifiersTracker.TrackIdentifier" />
    void IBrokerMessageIdentifiersTracker.TrackIdentifier(IBrokerMessageIdentifier identifier) => TrackOffset((KafkaOffset)identifier);

    /// <inheritdoc cref="IBrokerMessageIdentifiersTracker.GetCommitIdentifiers" />
    IReadOnlyCollection<IBrokerMessageIdentifier> IBrokerMessageIdentifiersTracker.GetCommitIdentifiers() => GetCommitOffsets();

    /// <inheritdoc cref="IBrokerMessageIdentifiersTracker.GetRollbackIdentifiers" />
    IReadOnlyCollection<IBrokerMessageIdentifier> IBrokerMessageIdentifiersTracker.GetRollbackIdentifiers() => GetRollbackOffSets();
}
