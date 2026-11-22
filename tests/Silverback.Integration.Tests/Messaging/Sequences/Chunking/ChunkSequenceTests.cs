// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public class ChunkSequenceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task AddAsync_ShouldPushChunksAndComplete_WhenLastChunkIsAdded()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ChunkSequence sequence = new("chunk-123", null, context);
        Task<byte[]> readTask = ReadAllAsync(sequence);

        AddToSequenceResult result1 = await sequence.AddAsync(CreateEnvelope(0, [1, 2, 3]), null, true).WaitAsync(Timeout);
        AddToSequenceResult result2 = await sequence.AddAsync(CreateEnvelope(1, [4, 5, 6]), null, true).WaitAsync(Timeout);
        AddToSequenceResult result3 = await sequence.AddAsync(CreateEnvelope(2, [7], true), null, true).WaitAsync(Timeout);

        result1.ShouldBe(AddToSequenceResult.Success(1));
        result2.ShouldBe(AddToSequenceResult.Success(1));
        result3.ShouldBe(AddToSequenceResult.Success(1));
        sequence.IsComplete.ShouldBeTrue();
        sequence.Length.ShouldBe(3);
        sequence.TotalLength.ShouldBe(3);
        (await readTask.WaitAsync(Timeout)).ShouldBe([1, 2, 3, 4, 5, 6, 7]);
    }

    [Fact]
    public async Task AddAsync_ShouldComplete_WhenTotalLengthIsReached()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ChunkSequence sequence = new("chunk-123", 2, context);
        Task<byte[]> readTask = ReadAllAsync(sequence);

        await sequence.AddAsync(CreateEnvelope(0, [1, 2, 3]), null, true).WaitAsync(Timeout);
        sequence.IsComplete.ShouldBeFalse();
        await sequence.AddAsync(CreateEnvelope(1, [4, 5]), null, true).WaitAsync(Timeout);

        sequence.IsComplete.ShouldBeTrue();
        sequence.Length.ShouldBe(2);
        (await readTask.WaitAsync(Timeout)).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task AddAsync_ShouldIgnoreDuplicateChunk()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ChunkSequence sequence = new("chunk-123", null, context);
        Task<byte[]> readTask = ReadAllAsync(sequence);

        await sequence.AddAsync(CreateEnvelope(0, [1, 2, 3]), null, true).WaitAsync(Timeout);
        AddToSequenceResult duplicateResult = await sequence.AddAsync(CreateEnvelope(0, [1, 2, 3]), null, true).WaitAsync(Timeout);
        sequence.Length.ShouldBe(1);
        await sequence.AddAsync(CreateEnvelope(1, [4, 5], true), null, true).WaitAsync(Timeout);

        duplicateResult.ShouldBe(AddToSequenceResult.Success(0));
        sequence.IsComplete.ShouldBeTrue();
        sequence.Length.ShouldBe(2);
        (await readTask.WaitAsync(Timeout)).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenChunkIndexHeaderIsMissing()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ChunkSequence sequence = new("chunk-123", null, context);

        Exception exception = await Should.ThrowAsync<InvalidOperationException>(
            sequence.AddAsync(CreateEnvelope(null, [1, 2, 3]), null, true));
        exception.Message.ShouldBe("Chunk index header not found.");
        sequence.Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task AddAsync_ShouldThrow_WhenFirstChunkIndexIsNotZero(int chunkIndex)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ChunkSequence sequence = new("chunk-123", null, context);

        Exception exception = await Should.ThrowAsync<SequenceException>(
            sequence.AddAsync(CreateEnvelope(chunkIndex, [1, 2, 3]), null, true));
        exception.Message.ShouldBe(
            $"Sequence error. Received chunk with index {chunkIndex} as first chunk for the sequence chunk-123, expected index 0. ");
        sequence.Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public async Task AddAsync_ShouldThrow_WhenChunkIndexIsOutOfOrder(int chunkIndex)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ChunkSequence sequence = new("chunk-123", null, context);
        Task<byte[]> readTask = ReadAllAsync(sequence);
        await sequence.AddAsync(CreateEnvelope(0, [1, 2, 3]), null, true).WaitAsync(Timeout);

        Exception exception = await Should.ThrowAsync<SequenceException>(
            sequence.AddAsync(CreateEnvelope(chunkIndex, [4, 5, 6]), null, true));
        exception.Message.ShouldBe($"Sequence error. Received chunk with index {chunkIndex} after index 0.");
        sequence.Length.ShouldBe(1);
        sequence.IsPending.ShouldBeTrue();

        await sequence.AbortAsync(SequenceAbortReason.Error, exception).WaitAsync(Timeout);
        await Should.ThrowAsync<OperationCanceledException>(readTask.WaitAsync(Timeout));
    }

    private static Task<byte[]> ReadAllAsync(ISequence sequence)
    {
        ChunkStream chunkStream = new(sequence.CreateStream<IRawInboundEnvelope>());

        return Task.Run(async () =>
        {
            await using (chunkStream)
            {
                using MemoryStream output = new();
                await chunkStream.CopyToAsync(output);
                return output.ToArray();
            }
        });
    }

    private static RawInboundEnvelope CreateEnvelope(int? chunkIndex, byte[] rawMessage, bool isLastChunk = false)
    {
        MessageHeaderCollection headers = [];

        if (chunkIndex != null)
            headers.Add(DefaultMessageHeaders.ChunkIndex, chunkIndex.Value);

        if (isLastChunk)
            headers.Add(DefaultMessageHeaders.IsLastChunk, true);

        return new RawInboundEnvelope(
            rawMessage,
            headers,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());
    }
}
