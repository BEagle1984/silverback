// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
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
        public async Task JoinIfComplete_AllChunks_Joined()
        {
            var headers = new MessageHeaderCollection();
            var originalMessage = new BinaryMessage
            {
                MessageId = Guid.NewGuid(),
                Content = GetByteArray(500)
            };

            var originalSerializedMessage = _serializer.Serialize(originalMessage, headers);

            var chunks = new InboundMessage[3];
            chunks[0] = new InboundMessage(
                originalSerializedMessage.AsMemory().Slice(0, 300).ToArray(),
                new[]
                {
                    new MessageHeader(MessageHeader.MessageIdKey, originalMessage.MessageId.ToString()),
                    new MessageHeader(MessageHeader.ChunkIdKey, "0"),
                    new MessageHeader(MessageHeader.ChunksCountKey, "3"),
                },
                null, TestConsumerEndpoint.GetDefault(), true);
            chunks[1] = new InboundMessage(
                originalSerializedMessage.AsMemory().Slice(300, 300).ToArray(),
                new[]
                {
                    new MessageHeader(MessageHeader.MessageIdKey, originalMessage.MessageId.ToString()),
                    new MessageHeader(MessageHeader.ChunkIdKey, "1"),
                    new MessageHeader(MessageHeader.ChunksCountKey, "3"),
                },
                null, TestConsumerEndpoint.GetDefault(), true);
            chunks[2] = new InboundMessage(
                originalSerializedMessage.AsMemory().Slice(600).ToArray(),
                new[]
                {
                    new MessageHeader(MessageHeader.MessageIdKey, originalMessage.MessageId.ToString()),
                    new MessageHeader(MessageHeader.ChunkIdKey, "2"),
                    new MessageHeader(MessageHeader.ChunksCountKey, "3"),
                },
                null, TestConsumerEndpoint.GetDefault(), true);

            var result = await new ChunkConsumer(_store).JoinIfComplete(chunks[0]);
            result.Should().BeNull();
            result = await new ChunkConsumer(_store).JoinIfComplete(chunks[1]);
            result.Should().BeNull();
            result = await new ChunkConsumer(_store).JoinIfComplete(chunks[2]);
            result.Should().NotBeNull();

            var deserializedResult = (BinaryMessage) _serializer.Deserialize(result, headers);
            deserializedResult.Content.Should().BeEquivalentTo(originalMessage.Content);
        }

        private byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte) 255).ToArray();
    }
}