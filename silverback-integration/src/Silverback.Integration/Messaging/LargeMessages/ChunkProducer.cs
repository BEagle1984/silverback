// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.LargeMessages
{
    public static class ChunkProducer
    {
        public static IEnumerable<(object message, byte[] serializedMessage)> ChunkIfNeeded(string messageId, object message, ChunkSettings settings, IMessageSerializer serializer)
        {
            var serializedMessage = serializer.Serialize(message);
            var chunkSize = settings?.Size ?? int.MaxValue;

            if (chunkSize >= serializedMessage.Length)
            {
                yield return (message, serializedMessage);
                yield break;
            }

            if (string.IsNullOrEmpty(messageId))
            {
                throw new InvalidOperationException(
                    "Dividing into chunks is pointless if no unique MessageId can be retrieved. " +
                    "Please add an Id or MessageId property to the message model or " +
                    "use a custom IMessageKeyProvider.");
            }

            var span = serializedMessage.AsMemory();
            var chunksCount = (int)Math.Ceiling(serializedMessage.Length / (double)chunkSize);
            var offset = 0;

            for (var i = 0; i < chunksCount; i++)
            {
                var messageChunk = new MessageChunk
                {
                    MessageId = Guid.NewGuid(),
                    OriginalMessageId = messageId,
                    ChunkId = i,
                    ChunksCount = chunksCount,
                    Content = span.Slice(offset, Math.Min(chunkSize, serializedMessage.Length - offset)).ToArray()
                };

                yield return (messageChunk, serializer.Serialize(messageChunk));

                offset += chunkSize;
            }
        }
    }
}