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
        public void CanHandleSequence_Chunk_TrueReturned()
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

            var result = reader.CanHandleSequence(envelope);

            result.Should().BeTrue();
        }

        [Fact]
        public void CanHandleSequence_NonChunk_FalseReturned()
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

            var result = reader.CanHandleSequence(envelope);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleSequence_FirstChunk_SequenceReturned()
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

            var sequence = await reader.HandleSequence(envelope);

            sequence.Should().NotBeNull();
            sequence!.Length.Should().Be(1);
            sequence!.TotalLength.Should().Be(4);
            using var enumerator = sequence.Stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().BeSameAs(envelope);
        }

        [Fact]
        public async Task HandleSequence_ChunkForExistingSequence_SequencePushed()
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

            var sequence1 = await reader.HandleSequence(envelope1);

            sequence1.Should().NotBeNull();
            sequence1!.Length.Should().Be(1);
            sequence1!.TotalLength.Should().Be(4);
            using var enumerator = sequence1.Stream.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().BeSameAs(envelope1);

            var sequence2 = await reader.HandleSequence(envelope2);

            sequence2.Should().BeNull();
            sequence1!.Length.Should().Be(2);
            enumerator.MoveNext();
            enumerator.Current.Should().BeSameAs(envelope2);
        }

        [Fact]
        public async Task HandleSequence_MissingFirstChunk_ChunkIgnored()
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

            var sequence = await reader.HandleSequence(envelope);

            sequence.Should().BeNull();
        }
    }
}
