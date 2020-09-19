// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking
{
    /// <summary>
    ///     Handles the <see cref="ChunksSequence"/> merging the chunks content into an envelope containing the unified <see cref="Stream"/>.
    /// </summary>
    public class ChunksAggregatorConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ChunksAggregator;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Sequence is ChunksSequence chunksSequence)
            {
                StartProcessingThread(context, next);

                await foreach (var chunkEnvelope in chunksSequence.Stream.ConfigureAwait(false))
                {
                    var chunkIndex = chunkEnvelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkIndex) ??
                                     throw new InvalidOperationException("Chunk index header not found.");

                    // Skip first chunk since the context.Envelope is already the one of the first chunk
                    if (chunkIndex == 0)
                        continue;

                    if (chunkEnvelope.RawMessage == null)
                        throw new InvalidOperationException("The chunk stream is null.");

                    await chunkEnvelope.RawMessage.CopyToAsync(context.Envelope.RawMessage).ConfigureAwait(false);
                }

                context.Envelope.RawMessage.
            }
        }

        private static void StartProcessingThread(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            // TODO: Handle transaction / exceptions
            Task.Factory.StartNew(
                async () => await next(context).ConfigureAwait(false),
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }
    }
}
