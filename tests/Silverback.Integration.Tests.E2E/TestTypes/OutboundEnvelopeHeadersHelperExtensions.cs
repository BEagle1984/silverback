// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes;

public static class OutboundEnvelopeHeadersHelperExtensions
{
    public static IOutboundEnvelope SetMessageTypeHeader(this IOutboundEnvelope envelope, Type messageType) =>
        envelope.AddOrReplaceHeader(DefaultMessageHeaders.MessageType, messageType.AssemblyQualifiedName);

    public static IOutboundEnvelope SetChunkHeaders(
        this IOutboundEnvelope envelope,
        string chunkMessageId,
        int chunkIndex,
        int chunksCount,
        Type? messageType = null,
        string? contentType = null) =>
        SetChunkHeadersCore(envelope, chunkMessageId, chunkIndex, chunksCount, null, messageType, contentType);

    public static IOutboundEnvelope SetChunkHeaders(
        this IOutboundEnvelope envelope,
        string chunkMessageId,
        int chunkIndex,
        bool isLastChunk,
        Type? messageType = null,
        string? contentType = null) =>
        SetChunkHeadersCore(envelope, chunkMessageId, chunkIndex, null, isLastChunk, messageType, contentType);

    public static IOutboundEnvelope SetChunkHeaders(
        this IOutboundEnvelope envelope,
        string chunkMessageId,
        int chunkIndex,
        Type? messageType = null,
        string? contentType = null) =>
        SetChunkHeadersCore(envelope, chunkMessageId, chunkIndex, null, null, messageType, contentType);

    private static IOutboundEnvelope SetChunkHeadersCore(
        IOutboundEnvelope envelope,
        string? chunkMessageId,
        int chunkIndex,
        int? chunksCount,
        bool? isLastChunk,
        Type? messageType,
        string? contentType)
    {
        if (chunkMessageId != null)
            envelope.AddHeader(DefaultMessageHeaders.ChunkMessageId, chunkMessageId);

        envelope.AddHeader(DefaultMessageHeaders.ChunkIndex, chunkIndex);

        if (chunksCount != null)
            envelope.AddHeader(DefaultMessageHeaders.ChunksCount, chunksCount);

        if (isLastChunk != null)
            envelope.AddHeader(DefaultMessageHeaders.IsLastChunk, isLastChunk);

        if (messageType != null)
            envelope.AddHeader(DefaultMessageHeaders.MessageType, messageType.AssemblyQualifiedName);

        if (contentType != null)
            envelope.AddHeader(DefaultMessageHeaders.ContentType, contentType);

        return envelope;
    }
}
