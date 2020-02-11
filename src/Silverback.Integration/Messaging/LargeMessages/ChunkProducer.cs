// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.LargeMessages
{
    internal static class ChunkProducer
    {
        public static IEnumerable<RawOutboundEnvelope> ChunkIfNeeded(RawOutboundEnvelope envelope)
        {
            var messageId = envelope.Headers.GetValue(DefaultMessageHeaders.MessageId);
            var settings = envelope.Endpoint?.Chunk;

            var chunkSize = settings?.Size ?? int.MaxValue;

            if (chunkSize >= envelope.RawMessage.Length)
            {
                yield return envelope;
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
                var messageChunk = new RawOutboundEnvelope(slice, envelope.Headers, envelope.Endpoint);

                messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunkId, i);
                messageChunk.Headers.AddOrReplace(DefaultMessageHeaders.ChunksCount, chunksCount);

                yield return messageChunk;

                offset += chunkSize;
            }
        }
    }
}