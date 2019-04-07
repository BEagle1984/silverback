// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.LargeMessages
{
    public class ChunkConsumerTests
    {
        private readonly IChunkStore _store = new InMemoryChunkStore();
        private readonly IMessageSerializer _serializer = new JsonMessageSerializer();

        [Fact]
        public void JoinIfComplete_AllChunks_Joined()
        {
            var originalMessage = new BinaryMessage
            {
                MessageId = Guid.NewGuid(),
                Content = GetByteArray(500)
            };

            var originalSerializedMessage = _serializer.Serialize(originalMessage);

            var chunks = new MessageChunk[3];
            chunks[0] = new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                ChunkId = 0,
                ChunksCount = 3,
                OriginalMessageId = originalMessage.MessageId.ToString(),
                Content = originalSerializedMessage.AsMemory().Slice(0, 300).ToArray()
            };
            chunks[1] = new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                ChunkId = 1,
                ChunksCount = 3,
                OriginalMessageId = originalMessage.MessageId.ToString(),
                Content = originalSerializedMessage.AsMemory().Slice(300, 300).ToArray()
            };
            chunks[2] = new MessageChunk
            {
                MessageId = Guid.NewGuid(),
                ChunkId = 2,
                ChunksCount = 3,
                OriginalMessageId = originalMessage.MessageId.ToString(),
                Content = originalSerializedMessage.AsMemory().Slice(600).ToArray()
            };

            var result = new ChunkConsumer(_store).JoinIfComplete(chunks[0]);
            result.Should().BeNull();
            result = new ChunkConsumer(_store).JoinIfComplete(chunks[1]);
            result.Should().BeNull();
            result = new ChunkConsumer(_store).JoinIfComplete(chunks[2]);
            result.Should().NotBeNull();

            var deserializedResult = (BinaryMessage)_serializer.Deserialize(result);
            deserializedResult.Content.Should().BeEquivalentTo(originalMessage.Content);
        }

        private byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte)255).ToArray();
    }
}