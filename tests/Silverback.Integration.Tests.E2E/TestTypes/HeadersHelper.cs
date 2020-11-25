// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public static class HeadersHelper
    {
        public static MessageHeader[] GetHeaders(string kafkaKey, Type? messageType = null) =>
            GetHeadersCore(null, kafkaKey, messageType).ToArray();

        public static MessageHeader[] GetChunkHeaders(
            string kafkaKey,
            int chunkIndex,
            int chunksCount,
            Type? messageType = null) =>
            GetChunkHeadersCore(null, kafkaKey, chunkIndex, chunksCount, null, messageType).ToArray();

        public static MessageHeader[] GetChunkHeaders(
            string kafkaKey,
            int chunkIndex,
            bool isLastChunk,
            Type? messageType = null) =>
            GetChunkHeadersCore(null, kafkaKey, chunkIndex, null, isLastChunk, messageType).ToArray();

        public static MessageHeader[] GetChunkHeaders(
            string kafkaKey,
            int chunkIndex,
            Type? messageType = null) =>
            GetChunkHeadersCore(null, kafkaKey, chunkIndex, null, null, messageType).ToArray();

        public static MessageHeader[] GetChunkHeadersWithMessageId(
            string messageId,
            int chunkIndex,
            int chunksCount,
            Type? messageType = null) =>
            GetChunkHeadersCore(messageId, null, chunkIndex, chunksCount, null, messageType).ToArray();

        public static MessageHeader[] GetChunkHeadersWithMessageId(
            string messageId,
            int chunkIndex,
            bool isLastChunk,
            Type? messageType = null) =>
            GetChunkHeadersCore(messageId, null, chunkIndex, null, isLastChunk, messageType).ToArray();

        public static MessageHeader[] GetChunkHeadersWithMessageId(
            string messageId,
            int chunkIndex,
            Type? messageType = null) =>
            GetChunkHeadersCore(messageId, null, chunkIndex, null, null, messageType).ToArray();

        private static IEnumerable<MessageHeader> GetHeadersCore(
            string? messageId,
            string? kafkaKey,
            Type? messageType)
        {
            if (!string.IsNullOrEmpty(messageId))
                yield return new MessageHeader(DefaultMessageHeaders.MessageId, messageId);

            if (!string.IsNullOrEmpty(kafkaKey))
                yield return new MessageHeader(KafkaMessageHeaders.KafkaMessageKey, kafkaKey);

            if (messageType != null)
                yield return new MessageHeader(DefaultMessageHeaders.MessageType, messageType.AssemblyQualifiedName);
        }

        private static IEnumerable<MessageHeader> GetChunkHeadersCore(
            string? messageId,
            string? kafkaKey,
            int chunkIndex,
            int? chunksCount,
            bool? isLastChunk,
            Type? messageType)
        {
            foreach (var header in GetHeadersCore(messageId, kafkaKey, messageType))
            {
                yield return header;
            }

            yield return new MessageHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex);

            if (chunksCount != null)
                yield return new MessageHeader(DefaultMessageHeaders.ChunksCount, chunksCount);

            if (isLastChunk != null)
                yield return new MessageHeader(DefaultMessageHeaders.IsLastChunk, isLastChunk.ToString());
        }
    }
}
