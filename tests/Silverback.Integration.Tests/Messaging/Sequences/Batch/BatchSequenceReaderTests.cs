// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Batch;

public class BatchSequenceReaderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void SortIndex_ShouldBeMaxValue()
    {
        BatchSequenceReader reader = new();

        // The batch is the last resort: the readers of the proper sequences (e.g. chunking) take precedence
        reader.SortIndex.ShouldBe(int.MaxValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanHandleAsync_ShouldReturnWhetherBatchIsEnabled(bool isBatchEnabled)
    {
        using ConsumerPipelineContext context = CreateContext(Substitute.For<ISequenceStore>(), isBatchEnabled);
        BatchSequenceReader reader = new();

        bool canHandle = await reader.CanHandleAsync(context);

        canHandle.ShouldBe(isBatchEnabled);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task GetSequenceAsync_ShouldCreateNewBatch_WhenNoBatchIsStored()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        SetupStoredBatch(sequenceStore, null);
        using ConsumerPipelineContext context = CreateContext(sequenceStore);
        BatchSequenceReader reader = new();

        using ISequence sequence = await reader.GetSequenceAsync(context);

        sequence.ShouldBeOfType<BatchSequence>();
        sequence.IsNew.ShouldBeTrue();
        sequence.SequenceId.ShouldStartWith("batch-");
        sequence.TotalLength.ShouldBe(10);
        await sequenceStore.Received(1).AddAsync(sequence);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task GetSequenceAsync_ShouldReturnCurrentBatch_WhenPending()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        using ConsumerPipelineContext context = CreateContext(sequenceStore);
        using BatchSequence pendingBatch = new("batch-1", context);
        SetupStoredBatch(sequenceStore, pendingBatch);
        BatchSequenceReader reader = new();

        ISequence sequence = await reader.GetSequenceAsync(context);

        sequence.ShouldBeSameAs(pendingBatch);
        await sequenceStore.DidNotReceive().AddAsync(Arg.Any<ISequence>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task GetSequenceAsync_ShouldCreateNewBatch_WhenCurrentBatchIsNotPending()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        using ConsumerPipelineContext context = CreateContext(sequenceStore);
        using BatchSequence abortedBatch = new("batch-1", context);
        await abortedBatch.AbortAsync(SequenceAbortReason.ConsumerAborted).WaitAsync(Timeout);
        SetupStoredBatch(sequenceStore, abortedBatch);
        BatchSequenceReader reader = new();

        using ISequence sequence = await reader.GetSequenceAsync(context);

        sequence.ShouldBeOfType<BatchSequence>();
        sequence.ShouldNotBeSameAs(abortedBatch);
        sequence.IsNew.ShouldBeTrue();
        await sequenceStore.Received(1).AddAsync(sequence);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task GetSequenceAsync_ShouldCreateNewBatch_WhenCurrentBatchIsCompleting()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        using ConsumerPipelineContext context = CreateContext(sequenceStore);
        using BatchSequence completingBatch = new("batch-1", context);
        await using IAsyncEnumerator<IInboundEnvelope> enumerator =
            completingBatch.CreateStream<IInboundEnvelope>().GetAsyncEnumerator();
        Task<AddToSequenceResult> addTask = completingBatch.AddAsync(CreateEnvelope(), null, true);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();

        // The batch is being completed (e.g. by its max wait time), waiting for the subscriber to acknowledge the pulled message
        Task completeTask = completingBatch.CompleteEarlyAsync();
        await AsyncTestingUtil.WaitAsync(() => completingBatch.IsCompleting);
        completingBatch.IsCompleting.ShouldBeTrue();
        completingBatch.IsPending.ShouldBeTrue();
        SetupStoredBatch(sequenceStore, completingBatch);
        BatchSequenceReader reader = new();

        using ISequence sequence = await reader.GetSequenceAsync(context);

        // The completing batch doesn't accept any further message
        sequence.ShouldBeOfType<BatchSequence>();
        sequence.ShouldNotBeSameAs(completingBatch);
        sequence.IsNew.ShouldBeTrue();

        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeFalse();
        await completeTask.WaitAsync(Timeout);
        (await addTask.WaitAsync(Timeout)).IsSuccess.ShouldBeTrue();
        completingBatch.IsComplete.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task GetSequenceAsync_ShouldCreateNewBatch_WhenCurrentBatchIsRemovedAfterBeingFound()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        using ConsumerPipelineContext context = CreateContext(sequenceStore);
        using BatchSequence pendingBatch = new("batch-1", context);

        // The batch is pending when checking whether a new one is needed, but it's removed (e.g. aborted) right afterwards
        sequenceStore.GetAsync<BatchSequence>(Arg.Any<string>(), true).Returns(pendingBatch);
        sequenceStore.GetAsync<ISequence>(Arg.Any<string>(), true).Returns((ISequence?)null);
        sequenceStore.AddAsync(Arg.Any<ISequence>()).Returns(call => ValueTask.FromResult(call.Arg<ISequence>()));

        BatchSequenceReader reader = new();

        ISequence sequence = await reader.GetSequenceAsync(context);

        // A new batch, instead of an incomplete sequence, which would skip the message (and commit it without processing it)
        sequence.ShouldBeOfType<BatchSequence>();
        sequence.ShouldNotBe(pendingBatch);
        sequence.IsNew.ShouldBeTrue();
        sequence.Dispose();
    }

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    private static void SetupStoredBatch(ISequenceStore sequenceStore, BatchSequence? storedBatch)
    {
        sequenceStore.GetAsync<BatchSequence>(Arg.Any<string>(), true).Returns(storedBatch);
        sequenceStore.GetAsync<ISequence>(Arg.Any<string>(), true).Returns(storedBatch);
        sequenceStore.AddAsync(Arg.Any<ISequence>()).Returns(call => ValueTask.FromResult(call.Arg<ISequence>()));
    }

    private static ConsumerPipelineContext CreateContext(ISequenceStore sequenceStore, bool isBatchEnabled = true)
    {
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = isBatchEnabled
                ? new BatchSettings
                {
                    Size = 10
                }
                : null
        };
        RawInboundEnvelope envelope = new(
            Stream.Null,
            [],
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        return ConsumerPipelineContextHelper.CreateSubstitute(envelope, sequenceStore: sequenceStore);
    }

    private static InboundEnvelope<TestEventOne> CreateEnvelope() =>
        new(
            new TestEventOne(),
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());
}
