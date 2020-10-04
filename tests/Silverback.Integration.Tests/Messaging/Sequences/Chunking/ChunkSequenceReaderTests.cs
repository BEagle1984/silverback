// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking
{
    public class ChunkSequenceReaderTests
    {
        [Fact]
        public void CanHandle_Chunk_TrueReturned()
        {
            var envelope = new RawInboundEnvelope(
                new byte[] { 0x01, 0x02, 0x03 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { DefaultMessageHeaders.ChunkIndex, "0" },
                    { DefaultMessageHeaders.ChunksCount, "4" }
                },
                new TestConsumerEndpoint("test"),
                "test");

            var reader = new ChunkSequenceReader(new DefaultSequenceStore<ChunkSequence>());

            var result = reader.CanHandle(ConsumerPipelineContextHelper.CreateSubstitute(envelope));

            result.Should().BeTrue();
        }

        [Fact]
        public void CanHandle_NonChunk_FalseReturned()
        {
            var envelope = new RawInboundEnvelope(
                new byte[] { 0x01, 0x02, 0x03 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                },
                new TestConsumerEndpoint("test"),
                "test");

            var reader = new ChunkSequenceReader(new DefaultSequenceStore<ChunkSequence>());

            var result = reader.CanHandle(ConsumerPipelineContextHelper.CreateSubstitute(envelope));

            result.Should().BeFalse();
        }

        [Fact]
        public void GetSequence_FirstChunk_SequenceReturned()
        {
            var envelope = new RawInboundEnvelope(
                new byte[] { 0x01, 0x02, 0x03 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { DefaultMessageHeaders.ChunkIndex, "0" },
                    { DefaultMessageHeaders.ChunksCount, "4" },
                },
                new TestConsumerEndpoint("test"),
                "test");

            var reader = new ChunkSequenceReader(new DefaultSequenceStore<ChunkSequence>());

            var sequence = reader.GetSequence(ConsumerPipelineContextHelper.CreateSubstitute(envelope), out var isNew);

            sequence.Should().NotBeNull();
            sequence!.TotalLength.Should().Be(4);
            isNew.Should().BeTrue();
        }

        [Fact]
        public void GetSequence_ChunkForExistingSequence_SequenceReturned()
        {
            var envelope1 = new RawInboundEnvelope(
                new byte[] { 0x01, 0x02, 0x03 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { DefaultMessageHeaders.ChunkIndex, "0" },
                    { DefaultMessageHeaders.ChunksCount, "4" },
                },
                new TestConsumerEndpoint("test"),
                "test");
            var envelope2 = new RawInboundEnvelope(
                new byte[] { 0x04, 0x05, 0x06 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { DefaultMessageHeaders.ChunkIndex, "1" },
                    { DefaultMessageHeaders.ChunksCount, "4" },
                },
                new TestConsumerEndpoint("test"),
                "test");

            var reader = new ChunkSequenceReader(new DefaultSequenceStore<ChunkSequence>());

            var sequence1 = reader.GetSequence(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope1),
                out var isNew);

            sequence1.Should().NotBeNull();
            sequence1.Should().BeOfType<ChunkSequence>();
            sequence1!.TotalLength.Should().Be(4);
            isNew.Should().BeTrue();

            var sequence2 = reader.GetSequence(ConsumerPipelineContextHelper.CreateSubstitute(envelope2), out isNew);

            sequence2.Should().NotBeNull();
            sequence2.Should().BeSameAs(sequence1);
            isNew.Should().BeFalse();
        }

        [Fact]
        public void GetSequence_MissingFirstChunk_NullReturned()
        {
            var envelope = new RawInboundEnvelope(
                new byte[] { 0x04, 0x05, 0x06 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { DefaultMessageHeaders.ChunkIndex, "1" },
                    { DefaultMessageHeaders.ChunksCount, "4" },
                },
                new TestConsumerEndpoint("test"),
                "test");

            var reader = new ChunkSequenceReader(new DefaultSequenceStore<ChunkSequence>());

            var sequence = reader.GetSequence(ConsumerPipelineContextHelper.CreateSubstitute(envelope), out _);

            sequence.Should().BeNull();
        }
    }
}
