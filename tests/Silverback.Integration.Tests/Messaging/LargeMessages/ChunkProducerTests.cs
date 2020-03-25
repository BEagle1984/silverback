// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.LargeMessages
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
            var headers = new MessageHeaderCollection();
            var serializedMessage = _serializer.Serialize(message, headers, MessageSerializationContext.Empty);
            var rawEnvelope =
                new RawOutboundEnvelope(headers,
                    new TestProducerEndpoint("test")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 500
                        }
                    })
                {
                    RawMessage = serializedMessage
                };

            var chunks = ChunkProducer.ChunkIfNeeded(rawEnvelope);

            chunks.Should().HaveCount(1);
            chunks.First().Should().BeEquivalentTo(rawEnvelope);
        }

        [Fact]
        public void ChunkIfNeeded_LargeMessage_Chunked()
        {
            var message = new BinaryMessage
            {
                MessageId = Guid.NewGuid(),
                Content = GetByteArray(1400)
            };
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "1234" }
            };

            var serializedMessage = _serializer.Serialize(message, headers, MessageSerializationContext.Empty);
            var rawEnvelope =
                new RawOutboundEnvelope(headers,
                    new TestProducerEndpoint("test")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 500
                        }
                    })
                {
                    RawMessage = serializedMessage
                };

            var chunks = ChunkProducer.ChunkIfNeeded(rawEnvelope).ToList();

            chunks.Should().HaveCount(4);
            chunks.Should().Match(c => c.All(m => m.RawMessage.Length < 1000));
        }

        private byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte) 255).ToArray();
    }
}