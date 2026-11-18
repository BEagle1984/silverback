// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The scope of the client side offset store. A scope is created per each consume cycle and is used to atomically store the processed
///     offsets.
/// </summary>
public sealed class KafkaOffsetStoreScope
{
    private readonly IKafkaOffsetStore _offsetStore;

    private readonly ConsumerPipelineContext _pipelineContext;

    private readonly string _groupId;

    private readonly Dictionary<TopicPartition, Offset> _lastStoredOffsets = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaOffsetStoreScope" /> class.
    /// </summary>
    /// <param name="offsetStore">
    ///     The <see cref="IKafkaOffsetStore" />.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    public KafkaOffsetStoreScope(IKafkaOffsetStore offsetStore, ConsumerPipelineContext context)
    {
        _pipelineContext = Check.NotNull(context);
        SilverbackContext = _pipelineContext.ServiceProvider.GetRequiredService<SilverbackContext>();
        _offsetStore = Check.NotNull(offsetStore);
        _groupId = (_pipelineContext.Consumer as KafkaConsumer)?.Configuration.GroupId ?? string.Empty;
    }

    /// <summary>
    ///     Gets the <see cref="ISilverbackContext" />.
    /// </summary>
    public ISilverbackContext SilverbackContext { get; }

    /// <summary>
    ///     Store all offsets that have been processed in this scope.
    /// </summary>
    /// <remarks>
    ///     Like the consumer commit, the offsets lower than the ones already committed (e.g., of an incomplete chunked message aborted
    ///     after the next messages were committed) and the offsets of the partitions being rolled back (which may be rolled back to a
    ///     preceding message) aren't stored. Unlike the consumer commit, the offsets are stored even if the partition was revoked while the
    ///     message was being processed: the message was processed and the offsets are stored atomically with the changes done by the
    ///     subscriber, so that it isn't processed again after the partition is assigned again.
    /// </remarks>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public async Task StoreOffsetsAsync()
    {
        IReadOnlyCollection<KafkaOffset> offsets = GetOffsetsToStore(includeRevokedPartitions: true);

        if (offsets.Count == 0 || AreAllStoredAlready(offsets))
            return;

        await _offsetStore.StoreOffsetsAsync(_groupId, offsets, SilverbackContext).ConfigureAwait(false);

        foreach (KafkaOffset offset in offsets)
        {
            _lastStoredOffsets[offset.TopicPartition] = offset.Offset;
        }
    }

    /// <summary>
    ///     Stores the offsets of the skipped messages outside the storage transaction enlisted by the subscribers, which failed with the
    ///     messages (see <see cref="Transaction.IConsumerTransactionManager.Skipping" />).
    /// </summary>
    /// <remarks>
    ///     The offsets are stored even if they were stored already in this scope, since they were stored in the failed transaction. Like the
    ///     consumer commit, the offsets lower than the ones already committed and the offsets of the partitions that aren't owned anymore
    ///     (e.g., revoked or being rolled back) aren't stored. Since the offsets are stored outside any transaction, the store can
    ///     complete after a concurrent store of a higher offset (e.g., when an incomplete chunked message is aborted by its timeout while
    ///     the next messages are committed), leaving the stored offset lower than the committed one until the next message is committed,
    ///     so that some messages may be consumed again after a restart or rebalance.
    /// </remarks>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    internal Task StoreSkippedOffsetsAsync()
    {
        IReadOnlyCollection<KafkaOffset> offsets = GetOffsetsToStore(includeRevokedPartitions: false);

        return offsets.Count == 0 ? Task.CompletedTask : _offsetStore.StoreOffsetsAsync(_groupId, offsets, context: null);
    }

    /// <summary>
    ///     Returns the offsets of the messages processed (or skipped) in this scope, formatted to be logged.
    /// </summary>
    /// <remarks>
    ///     They include the offsets that aren't stored because the consumer wouldn't commit them (see
    ///     <see cref="StoreSkippedOffsetsAsync" />), which can change in the meantime (e.g., when the partition is revoked).
    /// </remarks>
    /// <returns>
    ///     The offsets.
    /// </returns>
    internal string GetOffsetsToStoreLogString() =>
        string.Join(", ", _pipelineContext.GetCommitIdentifiers().Cast<KafkaOffset>().Select(offset => offset.ToVerboseLogString()));

    // Like the consumer commit, the offsets lower than the committed ones aren't stored (unless the partition was sought via Seek since
    // it was assigned), so that the stored offsets don't move back (e.g., when an incomplete chunked message is aborted after the next
    // messages were committed), and the offsets of the partitions being rolled back are never stored, since the rollback may seek back
    // to a preceding message. The offsets of the revoked partitions are stored only by the subscriber's storage transaction (and the
    // commit), which stores the processed messages atomically with their changes; like the consumer commit, the skip doesn't store them,
    // so that they're consumed again. The offsets are filtered right before each store, since the committed offsets and the assignment
    // change in the meantime.
    private IReadOnlyCollection<KafkaOffset> GetOffsetsToStore(bool includeRevokedPartitions)
    {
        IEnumerable<KafkaOffset> offsets = _pipelineContext.GetCommitIdentifiers().Cast<KafkaOffset>();

        return _pipelineContext.Consumer is KafkaConsumer consumer
            ? consumer.GetStorableOffsets(offsets, includeRevokedPartitions)
            : offsets.AsReadOnlyCollection();
    }

    private bool AreAllStoredAlready(IReadOnlyCollection<KafkaOffset> offsets) =>
        offsets.All(offset => _lastStoredOffsets.TryGetValue(offset.TopicPartition, out Offset storedOffset) &&
                              offset.Offset == storedOffset);
}
