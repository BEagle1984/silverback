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
        public async Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            var chunks = ChunkIfNeeded(context.Envelope).ToList();

            if (chunks.Any())
            {
                await chunks.ForEachAsync(chunkEnvelope =>
                    next(new ProducerPipelineContext(chunkEnvelope, context.Producer)));
            }
            else
            {
                await next(context);
            }
        }

        private IEnumerable<IOutboundEnvelope> ChunkIfNeeded(IOutboundEnvelope envelope)
        {
            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
            var settings = envelope.Endpoint?.Chunk;

            var chunkSize = settings?.Size ?? int.MaxValue;

            if (envelope.RawMessage == null || chunkSize >= envelope.RawMessage.Length)
            {
                yield break;
            }

            if (string.IsNullOrEmpty(messageId))
            {
                throw new InvalidOperationException(
                    "Dividing into chunks is pointless if no unique MessageId can be retrieved. " +
                    "Please add an Id or MessageId property to the message model or use a custom IMessageIdProvider.");
            }

            var span = envelope.RawMessage.AsMemory();
            var chunksCount = (int) Math.Ceiling(envelope.RawMessage.Length / (double) chunkSize);
            var offset = 0;

            for (var i = 0; i < chunksCount; i++)
            {
                var slice = span.Slice(offset, Math.Min(chunkSize, envelope.RawMessage.Length - offset)).ToArray();
                var messageChunk = new OutboundEnvelope(envelope.Message, envelope.Headers, envelope.Endpoint)
                {
                    RawMessage = slice
                };

                messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunkId, i);
                messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

                yield return messageChunk;

                offset += chunkSize;
            }
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.ChunkSplitter;
    }
}