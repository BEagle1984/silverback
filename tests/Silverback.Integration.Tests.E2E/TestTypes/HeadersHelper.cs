// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes;

public static class HeadersHelper
{
    public static MessageHeader[] GetHeaders(string kafkaKey, Type? messageType = null, string? contentType = null) =>
        GetHeadersCore(kafkaKey, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeaders(
        string chunkMessageId,
        int chunkIndex,
        int chunksCount,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(chunkMessageId, null, chunkIndex, chunksCount, null, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeadersWithKafkaKey(
        string chunkMessageId,
        int chunkIndex,
        int chunksCount,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(chunkMessageId, chunkMessageId, chunkIndex, chunksCount, null, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeaders(
        string chunkMessageId,
        int chunkIndex,
        bool isLastChunk,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(chunkMessageId, null, chunkIndex, null, isLastChunk, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeadersWithKafkaKey(
        string chunkMessageId,
        int chunkIndex,
        bool isLastChunk,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(chunkMessageId, chunkMessageId, chunkIndex, null, isLastChunk, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeaders(
        string chunkMessageId,
        int chunkIndex,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(chunkMessageId, null, chunkIndex, null, null, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeadersWithKafkaKey(
        string chunkMessageId,
        int chunkIndex,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(chunkMessageId, chunkMessageId, chunkIndex, null, null, messageType, contentType).ToArray();

    private static IEnumerable<MessageHeader> GetHeadersCore(string? kafkaKey, Type? messageType, string? contentType)
    {
        if (!string.IsNullOrEmpty(kafkaKey))
            yield return new MessageHeader(KafkaMessageHeaders.MessageKey, kafkaKey);

        if (messageType != null)
            yield return new MessageHeader(DefaultMessageHeaders.MessageType, messageType.AssemblyQualifiedName);

        if (contentType != null)
            yield return new MessageHeader(DefaultMessageHeaders.ContentType, contentType);
    }

    private static IEnumerable<MessageHeader> GetChunkHeadersCore(
        string? chunkMessageId,
        string? kafkaKey,
        int chunkIndex,
        int? chunksCount,
        bool? isLastChunk,
        Type? messageType,
        string? contentType)
    {
        foreach (MessageHeader header in GetHeadersCore(kafkaKey, messageType, contentType))
            yield return header;

        yield return new MessageHeader(DefaultMessageHeaders.ChunkMessageId, chunkMessageId);
        yield return new MessageHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex.ToString(CultureInfo.InvariantCulture));

        if (chunksCount != null)
            yield return new MessageHeader(DefaultMessageHeaders.ChunksCount, chunksCount.Value.ToString(CultureInfo.InvariantCulture));

        if (isLastChunk != null)
            yield return new MessageHeader(DefaultMessageHeaders.IsLastChunk, isLastChunk.ToString());
    }
}
