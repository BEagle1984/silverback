// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking
{
    public class ChunkSequenceWriterTests
    {
        [Fact]
        public void MustCreateSequence_MessageExceedsChunkSize_TrueReturned()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10 };
            var envelope = new OutboundEnvelope(
                rawMessage,
                null,
                new TestProducerEndpoint("test")
                {
                    Chunk = new ChunkSettings
                    {
                        Size = 3
                    }
                });

            var writer = new ChunkSequenceWriter();
            var result = writer.CanHandle(envelope);

            result.Should().BeTrue();
        }

        [Fact]
        public void MustCreateSequence_MessageSmallerThanChunkSize_FalseReturned()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10 };
            var envelope = new OutboundEnvelope(
                rawMessage,
                null,
                new TestProducerEndpoint("test")
                {
                    Chunk = new ChunkSettings
                    {
                        Size = 10
                    }
                });

            var writer = new ChunkSequenceWriter();
            var result = writer.CanHandle(envelope);

            result.Should().BeFalse();
        }

        [Fact]
        public void MustCreateSequence_NoChunking_FalseReturned()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10 };
            var envelope = new OutboundEnvelope(
                rawMessage,
                null,
                new TestProducerEndpoint("test"));

            var writer = new ChunkSequenceWriter();
            var result = writer.CanHandle(envelope);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ProcessMessage_LargeMessage_ChunkEnvelopesReturned()
        {
            var rawMessage = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10 };
            var sourceEnvelope = new OutboundEnvelope(
                rawMessage,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { "some-custom-header", "abc" }
                },
                new TestProducerEndpoint("test")
                {
                    Chunk = new ChunkSettings
                    {
                        Size = 3
                    }
                },
                true);

            var writer = new ChunkSequenceWriter();
            var envelopes = await writer.ProcessMessage(sourceEnvelope).ToListAsync();

            envelopes.Count.Should().Be(4);
            envelopes.ForEach(envelope => envelope.Endpoint.Should().BeSameAs(sourceEnvelope.Endpoint));
            envelopes.ForEach(envelope => envelope.Headers.Should().Contain(sourceEnvelope.Headers));
            envelopes.ForEach(envelope => envelope.AutoUnwrap.Should().Be(sourceEnvelope.AutoUnwrap));
            envelopes[0].RawMessage.ReadAll().Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
            envelopes[0].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"));
            envelopes[0].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
            envelopes[1].RawMessage.ReadAll().Should().BeEquivalentTo(new byte[] { 0x04, 0x05, 0x06 });
            envelopes[1].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"));
            envelopes[1].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
            envelopes[2].RawMessage.ReadAll().Should().BeEquivalentTo(new byte[] { 0x07, 0x08, 0x09 });
            envelopes[2].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"));
            envelopes[2].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
            envelopes[3].RawMessage.ReadAll().Should().BeEquivalentTo(new byte[] { 0x10 });
            envelopes[3].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "3"));
            envelopes[3].Headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        }
    }
}
