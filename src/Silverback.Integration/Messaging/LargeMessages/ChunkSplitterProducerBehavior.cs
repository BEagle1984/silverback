// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     Splits the messages into chunks according to the <see cref="ChunkSettings" />.
    /// </summary>
    public class ChunkSplitterProducerBehavior : IProducerBehavior, ISorted
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.ChunkSplitter;

        /// <inheritdoc cref="IProducerBehavior.Handle" />
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var chunks = ChunkIfNeeded(context.Envelope).ToList();

            if (chunks.Any())
            {
                string? firstOffsetValue = null;

                for (int i = 0; i < chunks.Count; i++)
                {
                    if (i > 0 && firstOffsetValue != null)
                        chunks[i].Headers.Add(DefaultMessageHeaders.FirstChunkOffset, firstOffsetValue);

                    await next(new ProducerPipelineContext(chunks[i], context.Producer));

                    if (i == 0)
                        firstOffsetValue = chunks[0].Offset?.Value;
                }
            }
            else
            {
                await next(context);
            }
        }

        private static IEnumerable<IOutboundEnvelope> ChunkIfNeeded(IOutboundEnvelope envelope)
        {
            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
            var settings = envelope.Endpoint.Chunk;

            var chunkSize = settings?.Size ?? int.MaxValue;

            if (envelope.RawMessage == null || chunkSize >= envelope.RawMessage.Length)
                yield break;

            if (string.IsNullOrEmpty(messageId))
            {
                throw new InvalidOperationException(
                    "Dividing into chunks is pointless if no unique MessageId can be retrieved. " +
                    $"Please set the {DefaultMessageHeaders.MessageId} header.");
            }

            var messageMemory = envelope.RawMessage.AsMemory();
            var chunksCount = (int)Math.Ceiling(envelope.RawMessage.Length / (double)chunkSize);
            var offset = 0;

            for (var i = 0; i < chunksCount; i++)
            {
                var memorySlice = messageMemory.Slice(offset, Math.Min(chunkSize, envelope.RawMessage.Length - offset));

                var messageChunk = new OutboundEnvelope(envelope.Message, envelope.Headers, envelope.Endpoint)
                {
                    RawMessage = memorySlice.ToArray()
                };

                messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunkIndex, i);
                messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

                yield return messageChunk;

                offset += chunkSize;
            }
        }
    }
}
