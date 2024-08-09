// Copyright (c) 2024 Sergio Aquilini
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
        _pipelineContext = Check.NotNull(context, nameof(context));
        SilverbackContext = _pipelineContext.ServiceProvider.GetRequiredService<SilverbackContext>();
        _offsetStore = Check.NotNull(offsetStore, nameof(offsetStore));
        _groupId = (_pipelineContext.Consumer as KafkaConsumer)?.Configuration.GroupId ?? string.Empty;
    }

    /// <summary>
    ///     Gets the <see cref="ISilverbackContext" />.
    /// </summary>
    public ISilverbackContext SilverbackContext { get; }

    /// <summary>
    ///     Store all offsets that have been processed in this scope.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public async Task StoreOffsetsAsync()
    {
        IReadOnlyCollection<KafkaOffset> offsets = _pipelineContext.GetCommitIdentifiers().Cast<KafkaOffset>().AsReadOnlyCollection();

        if (offsets.Count == 0 || AreAllStoredAlready(offsets))
            return;

        await _offsetStore.StoreOffsetsAsync(_groupId, offsets, SilverbackContext).ConfigureAwait(false);

        foreach (KafkaOffset offset in offsets)
        {
            _lastStoredOffsets[offset.TopicPartition] = offset.Offset;
        }
    }

    private bool AreAllStoredAlready(IReadOnlyCollection<KafkaOffset> offsets) =>
        offsets.All(
            offset => _lastStoredOffsets.TryGetValue(offset.TopicPartition, out Offset storedOffset) &&
                      offset.Offset == storedOffset);
}
