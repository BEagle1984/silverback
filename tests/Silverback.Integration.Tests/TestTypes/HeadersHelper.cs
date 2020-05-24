// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public static class HeadersHelper
    {
        public static MessageHeader[] GetChunkHeaders<TMessage>(string messageId, int chunkIndex, int chunksCount) =>
            new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, messageId),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, chunksCount),
                new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TMessage).AssemblyQualifiedName)
            };

        public static MessageHeader[] GetChunkHeaders(string messageId, int chunkIndex, int chunksCount) =>
            new[]
            {
                new MessageHeader(DefaultMessageHeaders.MessageId, messageId),
                new MessageHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex),
                new MessageHeader(DefaultMessageHeaders.ChunksCount, chunksCount),
            };
    }
}
