﻿// Copyright (c) 2019 Sergio Aquilini
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
            var serializedMessage = _serializer.Serialize(message, headers);
            var rawBrokerMessage =
                new RawOutboundMessage(message, headers,
                    new TestProducerEndpoint("test")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 500
                        }
                    })
                {
                    RawContent = serializedMessage
                };

            var chunks = ChunkProducer.ChunkIfNeeded(rawBrokerMessage);

            chunks.Should().HaveCount(1);
            chunks.First().Should().BeEquivalentTo(rawBrokerMessage);
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
                { MessageHeader.MessageIdKey, "1234" }
            };

            var serializedMessage = _serializer.Serialize(message, headers);
            var rawBrokerMessage =
                new RawOutboundMessage(message, headers,
                    new TestProducerEndpoint("test")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 500
                        }
                    })
                {
                    RawContent = serializedMessage
                };

            var chunks = ChunkProducer.ChunkIfNeeded(rawBrokerMessage);

            chunks.Should().HaveCount(4);
            chunks.Should().Match(c => c.All(m => m.RawContent.Length < 1000));
        }

        private byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte) 255).ToArray();
    }
}
