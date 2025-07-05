// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public sealed class ChunkSequenceReaderFixture : IDisposable
{
    private readonly ISequenceStore _sequenceStore =
        new SequenceStore(new SilverbackLoggerSubstitute<SequenceStore>());

    [Fact]
    public async Task CanHandle_ShouldReturnTrue_WhenAllHeadersWithCountAreSet()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanHandle_ShouldReturnTrue_WhenAllHeadersWithIsLastChunkAreSet()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.IsLastChunk, "0" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanHandle_ShouldReturnFalse_WhenChunkMessageIdMissing()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanHandle_ShouldReturnFalse_WhenChunkIndexMissing()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanHandle_ShouldReturnFalse_WhenChunkCountIsOne()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "1" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetSequence_ShouldReturnNewSequence_WhenFirstChunk()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        ISequence sequence = await new ChunkSequenceReader().GetSequenceAsync(context);

        sequence.ShouldNotBeNull();
        sequence.SequenceId.ShouldBe("chunk-123");
        sequence.TotalLength.ShouldBe(4);
        sequence.IsNew.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSequence_ShouldReturnExistingSequence()
    {
        RawInboundEnvelope envelope1 = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());
        RawInboundEnvelope envelope2 = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "1" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ChunkSequenceReader reader = new();

        ConsumerPipelineContext context1 = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope1,
            sequenceStore: _sequenceStore);

        ISequence sequence1 = await reader.GetSequenceAsync(context1);

        sequence1.ShouldNotBeNull();
        sequence1.ShouldBeOfType<ChunkSequence>();
        sequence1.TotalLength.ShouldBe(4);
        sequence1.IsNew.ShouldBeTrue();

        ConsumerPipelineContext context2 = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope2,
            sequenceStore: _sequenceStore);

        ISequence sequence2 = await reader.GetSequenceAsync(context2);

        sequence2.ShouldNotBeNull();
        sequence2.ShouldBeSameAs(sequence1);
        sequence2.IsNew.ShouldBeFalse();
    }

    [Fact]
    public async Task GetSequence_ShouldReturnIncompleteSequence_WhenFirstChunkMissing()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkMessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "1" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _sequenceStore);

        ISequence sequence = await new ChunkSequenceReader().GetSequenceAsync(context);

        sequence.ShouldBeOfType<IncompleteSequence>();
    }

    public void Dispose() => _sequenceStore.DisposeAsync().SafeWait();
}
