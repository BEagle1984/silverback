// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public static class HeadersHelper
    {
        public static MessageHeader[] GetChunkHeaders<TMessage>(
            string messageId,
            int chunkIndex,
            int? chunksCount,
            bool? isLastChunk = null) =>
            GetChunkHeadersCore<TMessage>(messageId, chunkIndex, chunksCount, isLastChunk).ToArray();

        public static MessageHeader[] GetChunkHeaders<TMessage>(int chunkIndex, int chunksCount) =>
            new[]
            {
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, chunksCount),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TMessage).AssemblyQualifiedName)
            };

        public static IEnumerable<MessageHeader> GetChunkHeadersCore<TMessage>(
            string messageId,
            int chunkIndex,
            int? chunksCount,
            bool? isLastChunk = null)
        {
            yield return new MessageHeader(DefaultMessageHeaders.MessageId, messageId);
            yield return new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TMessage).AssemblyQualifiedName);
            yield return new MessageHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex);

            if (chunksCount != null)
                yield return new MessageHeader(DefaultMessageHeaders.ChunksCount, chunksCount);

            if (isLastChunk != null)
                yield return new MessageHeader(DefaultMessageHeaders.IsLastChunk, isLastChunk.ToString());
        }
    }
}
