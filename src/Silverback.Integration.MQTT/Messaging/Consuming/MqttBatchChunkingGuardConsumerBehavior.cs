// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Consuming;

/// <summary>
///     Rejects the chunked messages consumed by a client with batch processing enabled. MQTT can't seek, so the chunks are replayed from
///     memory and chunking can't be combined with batch processing.
/// </summary>
/// <remarks>
///     The behavior runs after the <see cref="TransactionHandlerConsumerBehavior" />, so that the error policies apply, and before the
///     <see cref="RawSequencerConsumerBehavior" />, so that the chunk sequence isn't started.
/// </remarks>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Class used via DI")]
internal sealed class MqttBatchChunkingGuardConsumerBehavior : IConsumerBehavior
{
    private static readonly ChunkSequenceReader ChunkSequenceReader = new();

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.RawSequencer - 10;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Consumer is MqttConsumer { Configuration.BatchSize: not null } &&
            await ChunkSequenceReader.CanHandleAsync(context).ConfigureAwait(false))
        {
            throw new SequenceException(
                "Chunked messages can't be consumed with batch processing over MQTT. " +
                "Consume them with a client without batch processing, or disable chunking in the producer.");
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
