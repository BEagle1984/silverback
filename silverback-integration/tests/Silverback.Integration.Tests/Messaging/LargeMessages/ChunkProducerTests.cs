using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.LargeMessages
{
    public class ChunkProducerTests
    {
        private readonly IMessageSerializer _serializer = new JsonMessageSerializer();

        [Fact]
        public void ChunkIfNeeded_SmallMessage_ReturnedWithoutChanges()
        {
            var message = new BinaryMessage
            {
                MessageId = Guid.NewGuid(),
                Content = GetByteArray(100)
            };
            var serializedMessage = _serializer.Serialize(message);

            var chunks = new ChunkProducer().ChunkIfNeeded(message.MessageId.ToString(), message, new ChunkSettings
            {
                Size = 500
            }, _serializer);

            chunks.Should().HaveCount(1);
            chunks.First().Should().BeEquivalentTo((message, serializedMessage));
        }

        [Fact]
        public void ChunkIfNeeded_LargeMessage_Chunked()
        {
            var message = new BinaryMessage
            {
                MessageId = Guid.NewGuid(),
                Content = GetByteArray(1400)
            };
            var serializedMessage = _serializer.Serialize(message);

            var chunks = new ChunkProducer().ChunkIfNeeded(message.MessageId.ToString(), message, new ChunkSettings
            {
                Size = 500
            }, _serializer);

            chunks.Should().HaveCount(5);
            chunks.Should().Match(c => c.All(t => t.Item1 is MessageChunk));
            chunks.Should().Match(c => c.All(t => t.Item2.Length < 1000));
        }

        private byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte) 255).ToArray();
    }
}
