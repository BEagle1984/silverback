// Copyright (c) 2023 Sergio Aquilini
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
        GetHeadersCore(null, kafkaKey, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeaders(
        string kafkaKey,
        int chunkIndex,
        int chunksCount,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(null, kafkaKey, chunkIndex, chunksCount, null, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeaders(
        string kafkaKey,
        int chunkIndex,
        bool isLastChunk,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(null, kafkaKey, chunkIndex, null, isLastChunk, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeaders(
        string kafkaKey,
        int chunkIndex,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(null, kafkaKey, chunkIndex, null, null, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeadersWithMessageId(
        string messageId,
        int chunkIndex,
        int chunksCount,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(messageId, null, chunkIndex, chunksCount, null, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeadersWithMessageId(
        string messageId,
        int chunkIndex,
        bool isLastChunk,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(messageId, null, chunkIndex, null, isLastChunk, messageType, contentType).ToArray();

    public static MessageHeader[] GetChunkHeadersWithMessageId(
        string messageId,
        int chunkIndex,
        Type? messageType = null,
        string? contentType = null) =>
        GetChunkHeadersCore(messageId, null, chunkIndex, null, null, messageType, contentType).ToArray();

    private static IEnumerable<MessageHeader> GetHeadersCore(string? messageId, string? kafkaKey, Type? messageType, string? contentType)
    {
        if (!string.IsNullOrEmpty(messageId))
            yield return new MessageHeader(DefaultMessageHeaders.MessageId, messageId);

        if (!string.IsNullOrEmpty(kafkaKey))
            yield return new MessageHeader(DefaultMessageHeaders.MessageId, kafkaKey);

        if (messageType != null)
            yield return new MessageHeader(DefaultMessageHeaders.MessageType, messageType.AssemblyQualifiedName);

        if (contentType != null)
            yield return new MessageHeader(DefaultMessageHeaders.ContentType, contentType);
    }

    private static IEnumerable<MessageHeader> GetChunkHeadersCore(
        string? messageId,
        string? kafkaKey,
        int chunkIndex,
        int? chunksCount,
        bool? isLastChunk,
        Type? messageType,
        string? contentType)
    {
        foreach (MessageHeader header in GetHeadersCore(messageId, kafkaKey, messageType, contentType))
        {
            yield return header;
        }

        yield return new MessageHeader(
            DefaultMessageHeaders.ChunkIndex,
            chunkIndex.ToString(CultureInfo.InvariantCulture));

        if (chunksCount != null)
        {
            yield return new MessageHeader(
                DefaultMessageHeaders.ChunksCount,
                chunksCount.ToString());
        }

        if (isLastChunk != null)
            yield return new MessageHeader(DefaultMessageHeaders.IsLastChunk, isLastChunk.ToString());
    }
}
