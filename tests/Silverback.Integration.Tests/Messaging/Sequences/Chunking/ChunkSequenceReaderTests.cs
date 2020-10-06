// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking
{
    public class ChunkSequenceReaderTests
    {
        private readonly ISequenceStore _defaultSequenceStore = new DefaultSequenceStore();

        private readonly IConsumer _consumerSubstitute;

        public ChunkSequenceReaderTests()
        {
            _consumerSubstitute = Substitute.For<IConsumer>();
            _consumerSubstitute.SequenceStore.Returns(_defaultSequenceStore);
        }

        [Fact]
        public async Task CanHandle_Chunk_TrueReturned()
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
                "test",
                new TestOffset());

            var context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, consumer: _consumerSubstitute);
            var result = await new ChunkSequenceReader().CanHandleAsync(context);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanHandle_NonChunk_FalseReturned()
        {
            var envelope = new RawInboundEnvelope(
                new byte[] { 0x01, 0x02, 0x03 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                },
                new TestConsumerEndpoint("test"),
                "test",
                new TestOffset());

            var context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, consumer: _consumerSubstitute);
            var result = await new ChunkSequenceReader().CanHandleAsync(context);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetSequence_FirstChunk_SequenceReturned()
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
                "test",
                new TestOffset());

            var context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, consumer: _consumerSubstitute);
            var sequence = await new ChunkSequenceReader().GetSequenceAsync(context);

            sequence.Should().NotBeNull();
            sequence!.TotalLength.Should().Be(4);
            sequence.IsNew.Should().BeTrue();
        }

        [Fact]
        public async Task GetSequence_ChunkForExistingSequence_SequenceReturned()
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
                "test",
                new TestOffset());
            var envelope2 = new RawInboundEnvelope(
                new byte[] { 0x04, 0x05, 0x06 },
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageId, "123" },
                    { DefaultMessageHeaders.ChunkIndex, "1" },
                    { DefaultMessageHeaders.ChunksCount, "4" },
                },
                new TestConsumerEndpoint("test"),
                "test",
                new TestOffset());

            var reader = new ChunkSequenceReader();

            var context1 = ConsumerPipelineContextHelper.CreateSubstitute(envelope1, consumer: _consumerSubstitute);
            var sequence1 = await reader.GetSequenceAsync(context1);

            sequence1.Should().NotBeNull();
            sequence1.Should().BeOfType<ChunkSequence>();
            sequence1!.TotalLength.Should().Be(4);
            sequence1.IsNew.Should().BeTrue();

            var context2 = ConsumerPipelineContextHelper.CreateSubstitute(envelope2, consumer: _consumerSubstitute);
            var sequence2 = await reader.GetSequenceAsync(context2);

            sequence2.Should().NotBeNull();
            sequence2.Should().BeSameAs(sequence1);
            sequence2!.IsNew.Should().BeFalse();
        }

        [Fact]
        public async Task GetSequence_MissingFirstChunk_NullReturned()
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
                "test",
                new TestOffset());

            var context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, consumer: _consumerSubstitute);
            var sequence = await new ChunkSequenceReader().GetSequenceAsync(context);

            sequence.Should().BeNull();
        }
    }
}
