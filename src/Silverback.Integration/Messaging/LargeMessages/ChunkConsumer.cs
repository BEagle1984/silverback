// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.LargeMessages
{
    public class ChunkConsumer
    {
        private readonly IChunkStore _store;
 
        public ChunkConsumer(IChunkStore store)
        {
            _store = store;
        }

        public byte[] JoinIfComplete(IInboundMessage message)
        {
            var (messageId, chunkId, chunksCount) = ExtractHeadersValues(message);

            var count = _store.CountChunks(messageId);

            if (count >= chunksCount - 1)
            {
                var chunks = _store.GetChunks(messageId);
                if (chunks.ContainsKey(chunkId))
                    return null;

                chunks.Add(chunkId, message.RawContent);

                var completeMessage = Join(chunks);

                _store.Cleanup(messageId);

                return completeMessage;
            }
            else
            {
                _store.Store(messageId, chunkId, chunksCount, message.RawContent);
                return null;
            }
        }

        private (string messageId, int chinkId, int chunksCount) ExtractHeadersValues(IInboundMessage message)
        {
            var messageId = message.Headers.GetValue(MessageHeader.MessageIdKey);

            var chunkId = message.Headers.GetValue<int>(MessageHeader.ChunkIdKey);

            var chunksCount = message.Headers.GetValue<int>(MessageHeader.ChunksCountKey);

            if (string.IsNullOrEmpty(messageId))
                throw new InvalidOperationException("Message id header not found or invalid.");

            if (chunkId == null)
                throw new InvalidOperationException("Chunk id header not found or invalid.");

            if (chunksCount == null)
                throw new InvalidOperationException("Chunks count header not found or invalid.");

            return (messageId, chunkId.Value, chunksCount.Value);
        }

        public void Commit() => _store.Commit();

        public void Rollback() => _store.Rollback();

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
