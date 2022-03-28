// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public sealed class ChunkSequenceReaderTests : IDisposable
{
    private readonly ISequenceStore _defaultSequenceStore =
        new DefaultSequenceStore(new SilverbackLoggerSubstitute<DefaultSequenceStore>());

    [Fact]
    public async Task CanHandle_Chunk_TrueReturned()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _defaultSequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanHandle_NonChunk_FalseReturned()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "123" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _defaultSequenceStore);

        bool result = await new ChunkSequenceReader().CanHandleAsync(context);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetSequence_FirstChunk_SequenceReturned()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "0" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _defaultSequenceStore);

        ISequence sequence = await new ChunkSequenceReader().GetSequenceAsync(context);

        sequence.Should().NotBeNull();
        sequence.TotalLength.Should().Be(4);
        sequence.IsNew.Should().BeTrue();
    }

    [Fact]
    public async Task GetSequence_ChunkForExistingSequence_SequenceReturned()
    {
        RawInboundEnvelope envelope1 = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "123" },
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
                { DefaultMessageHeaders.MessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "1" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ChunkSequenceReader reader = new();

        ConsumerPipelineContext context1 = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope1,
            sequenceStore: _defaultSequenceStore);

        ISequence sequence1 = await reader.GetSequenceAsync(context1);

        sequence1.Should().NotBeNull();
        sequence1.Should().BeOfType<ChunkSequence>();
        sequence1.TotalLength.Should().Be(4);
        sequence1.IsNew.Should().BeTrue();

        ConsumerPipelineContext context2 = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope2,
            sequenceStore: _defaultSequenceStore);

        ISequence sequence2 = await reader.GetSequenceAsync(context2);

        sequence2.Should().NotBeNull();
        sequence2.Should().BeSameAs(sequence1);
        sequence2.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task GetSequence_MissingFirstChunk_IncompleteSequenceReturned()
    {
        RawInboundEnvelope envelope = new(
            BytesUtil.GetRandomBytes(10),
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "123" },
                { DefaultMessageHeaders.ChunkIndex, "1" },
                { DefaultMessageHeaders.ChunksCount, "4" }
            },
            new TestConsumerEndpointConfiguration("test").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            sequenceStore: _defaultSequenceStore);

        ISequence sequence = await new ChunkSequenceReader().GetSequenceAsync(context);

        sequence.Should().BeOfType<IncompleteSequence>();
    }

    public void Dispose()
    {
        AsyncHelper.RunSynchronously(() => _defaultSequenceStore.DisposeAsync().AsTask());
    }
}
