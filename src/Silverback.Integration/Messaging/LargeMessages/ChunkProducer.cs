// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.LargeMessages
{
    internal static class ChunkProducer
    {
        public static IEnumerable<OutboundMessage> ChunkIfNeeded(OutboundMessage message)
        {
            var messageId = message.Headers.GetValue(MessageHeader.MessageIdKey);
            var settings = (message.Endpoint as IProducerEndpoint)?.Chunk;

            var chunkSize = settings?.Size ?? int.MaxValue;

            if (chunkSize >= message.RawContent.Length)
            {
                yield return message;
                yield break;
            }

            if (string.IsNullOrEmpty(messageId))
            {
                throw new InvalidOperationException(
                    "Dividing into chunks is pointless if no unique MessageId can be retrieved. " +
                    "Please add an Id or MessageId property to the message model or " +
                    "use a custom IMessageKeyProvider.");
            }

            var span = message.RawContent.AsMemory();
            var chunksCount = (int)Math.Ceiling(message.RawContent.Length / (double)chunkSize);
            var offset = 0;

            for (var i = 0; i < chunksCount; i++)
            {
                var messageChunk = new OutboundMessage(message.Content, message.Headers, message.Endpoint)
                {
                    RawContent =
                        span.Slice(offset, Math.Min(chunkSize, message.RawContent.Length - offset)).ToArray()
                };

                messageChunk.Headers.AddOrReplace(MessageHeader.ChunkIdKey, i);
                messageChunk.Headers.AddOrReplace(MessageHeader.ChunksCountKey, chunksCount);

                yield return messageChunk;

                offset += chunkSize;
            }
        }
    }
}