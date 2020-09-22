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
    ///     Handles the <see cref="ChunkSequence"/> merging the chunks content into an envelope containing the unified <see cref="Stream"/>.
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

            if (context.Envelope.Sequence is ChunkSequence chunksSequence)
            {
                // TODO: No way to avoid the extra allocation?
                var envelope = new RawInboundEnvelope(
                    new ChunkStream(chunksSequence.Stream),
                    context.Envelope.Headers,
                    context.Envelope.Endpoint,
                    context.Envelope.ActualEndpointName,
                    context.Envelope.Offset,
                    context.Envelope.AdditionalLogData);

                var newContext = new ConsumerPipelineContext(envelope, context);

                await next(newContext).ConfigureAwait(false);
            }
            else
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }
}
