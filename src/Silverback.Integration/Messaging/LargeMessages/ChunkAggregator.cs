// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.LargeMessages
{
    public class ChunkAggregator
    {
        private readonly IChunkStore _store;

        public ChunkAggregator(IChunkStore store, ConsumerTransactionManager transactionManager)
        {
            _store = store;
            transactionManager.Enlist(_store);
        }

        public async Task<byte[]> AggregateIfComplete(IRawInboundEnvelope envelope)
        {
            var (messageId, chunkId, chunksCount) = ExtractHeadersValues(envelope);

            var count = await _store.CountChunks(messageId);

            if (count >= chunksCount - 1)
            {
                var chunks = await _store.GetChunks(messageId);
                if (chunks.ContainsKey(chunkId))
                    return null;

                chunks.Add(chunkId, envelope.RawMessage);

                var completeMessage = Join(chunks);

                await _store.Cleanup(messageId);

                return completeMessage;
            }
            else
            {
                await _store.Store(messageId, chunkId, chunksCount, envelope.RawMessage);
                return null;
            }
        }

        private (string messageId, int chinkId, int chunksCount) ExtractHeadersValues(IRawInboundEnvelope envelope)
        {
            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);

            var chunkId = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunkId);

            var chunksCount = envelope.Headers.GetValue<int>(DefaultMessageHeaders.ChunksCount);

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            if (chunkId == null)
                throw new InvalidOperationException("Chunk id header not found or invalid.");

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            return (messageId, chunkId.Value, chunksCount.Value);
        }

        public Task Commit() => _store.Commit();

        public Task Rollback() => _store.Rollback();

        private static byte[] Join(Dictionary<int, byte[]> chunks)
        {
            var buffer = new byte[chunks.Sum(c => c.Value.Length)];
            var offset = 0;
            foreach (var chunk2 in chunks.OrderBy(c => c.Key).Select(c => c.Value))
            {
                Buffer.BlockCopy(chunk2, 0, buffer, offset, chunk2.Length);
                offset += chunk2.Length;
            }

            return buffer;
        }
    }
}