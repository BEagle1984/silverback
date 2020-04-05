// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.LargeMessages
{
    public class ChunkSplitterProducerBehaviorTests
    {
        private readonly IMessageSerializer _serializer = new JsonMessageSerializer();

        [Fact]
        public void Handle_SmallMessage_ReturnedWithoutChanges()
        {
            var message = new BinaryMessage
            {
                MessageId = Guid.NewGuid(),
                Content = GetByteArray(100)
            };
            var headers = new MessageHeaderCollection();
            var serializedMessage = _serializer.Serialize(message, headers, MessageSerializationContext.Empty);
            var envelope =
                new OutboundEnvelope(message, headers,
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

            var chunks = new List<IOutboundEnvelope>();
            new ChunkSplitterProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>()),
                context =>
                {
                    chunks.Add(context.Envelope);
                    return Task.CompletedTask;
                });

            chunks.Should().HaveCount(1);
            chunks.First().Should().BeEquivalentTo(envelope);
        }

        [Fact]
        public void Handle_LargeMessage_SplitIntoChunks()
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
            var envelope =
                new OutboundEnvelope(message, headers,
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

            var chunks = new List<IOutboundEnvelope>();
            new ChunkSplitterProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, Substitute.For<IProducer>()),
                context =>
                {
                    chunks.Add(context.Envelope);
                    return Task.CompletedTask;
                });

            chunks.Should().HaveCount(4);
            chunks.Should().Match(c => c.All(m => m.RawMessage.Length < 1000));
        }

        private byte[] GetByteArray(int size) => Enumerable.Range(0, size).Select(_ => (byte) 255).ToArray();
    }
}