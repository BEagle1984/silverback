// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     Temporary stores and aggregates the message chunks to rebuild the original message.
    /// </summary>
    public class ChunkAggregatorConsumerBehavior : IConsumerBehavior
    {
        private readonly ConcurrentDictionary<IConsumer, List<IOffset>> _pendingOffsetsByConsumer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkAggregatorConsumerBehavior" /> class.
        /// </summary>
        public ChunkAggregatorConsumerBehavior()
        {
            _pendingOffsetsByConsumer = new ConcurrentDictionary<IConsumer, List<IOffset>>();
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ChunkAggregator;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            Check.NotNull(next, nameof(next));

            List<IOffset>? pendingOffsets = null;
            var chunkStore = serviceProvider.GetService<IChunkStore>();
            if (chunkStore != null)
                pendingOffsets = _pendingOffsetsByConsumer.GetOrAdd(context.Consumer, _ => new List<IOffset>());

            context.Envelopes =
                (await context.Envelopes.SelectAsync(envelope => AggregateIfNeeded(envelope, serviceProvider))
                    .ConfigureAwait(false))
                .WhereNotNull()
                .ToList();

            try
            {
                await TryHandle(context, serviceProvider, next, chunkStore, pendingOffsets).ConfigureAwait(false);
            }
            catch (Exception)
            {
                HandleOffsetRollback(context, pendingOffsets);

                throw;
            }
        }

        private static async Task TryHandle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next,
            IChunkStore? chunkStore,
            List<IOffset>? pendingOffsets)
        {
            if (context.Envelopes.Any())
                await next(context, serviceProvider).ConfigureAwait(false);

            if (chunkStore != null && chunkStore.HasNotPersistedChunks)
            {
                if (context.CommitOffsets != null)
                    pendingOffsets!.AddRange(context.CommitOffsets);

                context.CommitOffsets = null;
            }
            else if (pendingOffsets != null && pendingOffsets.Any())
            {
                if (context.CommitOffsets != null)
                    pendingOffsets.AddRange(context.CommitOffsets);

                context.CommitOffsets = pendingOffsets.ToList(); // Intentional clone
                pendingOffsets.Clear();
            }
        }

        private static void HandleOffsetRollback(ConsumerPipelineContext context, List<IOffset>? pendingOffsets)
        {
            // In case of exception all offsets must be rollback back (if a rollback takes place, so only
            // after all the error policies are applied -> since the actual offset rollback is driven by
            // the IErrorPolicyHelper the pendingOffsets list is not cleared yet)
            if (pendingOffsets != null && pendingOffsets.Any())
            {
                var clonedPendingOffsets = pendingOffsets.ToList();

                if (context.CommitOffsets != null)
                    clonedPendingOffsets.AddRange(context.CommitOffsets);

                context.CommitOffsets = clonedPendingOffsets;
            }
        }

        private static async Task<IRawInboundEnvelope?> AggregateIfNeeded(
            IRawInboundEnvelope envelope,
            IServiceProvider serviceProvider)
        {
            if (!envelope.Headers.Contains(DefaultMessageHeaders.ChunkIndex))
                return envelope;

            var chunkAggregator = serviceProvider.GetRequiredService<ChunkAggregator>();
            if (envelope.Headers.Contains(DefaultMessageHeaders.ChunksAggregated))
            {
                // If this is a retry the cleanup wasn't committed the run before
                await chunkAggregator.Cleanup(envelope).ConfigureAwait(false);

                return envelope;
            }

            var completeMessage = await chunkAggregator.AggregateIfComplete(envelope).ConfigureAwait(false);

            if (completeMessage == null)
                return null;

            var completeMessageEnvelope = new RawInboundEnvelope(
                completeMessage,
                envelope.Headers,
                envelope.Endpoint,
                envelope.ActualEndpointName,
                envelope.Offset);

            completeMessageEnvelope.Headers.Add(DefaultMessageHeaders.ChunksAggregated, true);

            return completeMessageEnvelope;
        }

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        private void OnRollback(ConsumingAbortedEvent message)
        {
            if (message.Context.CommitOffsets == null || !message.Context.CommitOffsets.Any())
                return;

            // Remove pending offsets when rolled back for real, just in case the consumer will be restarted
            // (not yet possible!)
            if (_pendingOffsetsByConsumer.TryGetValue(message.Context.Consumer, out var pendingOffsets))
                pendingOffsets.RemoveAll(offset => message.Context.CommitOffsets.Contains(offset));
        }
    }
}
