// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequencerConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task HandleAsync_ShouldReplaceSequence_WhenItKeepsRefusingMessageWithoutBeingAborted()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: sequenceStore);

        // A sequence disposed while still pending refuses every message without being aborted
        ISequence refusingSequence = CreateSequence("refusing", AddToSequenceResult.Failed);
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        bool isRefusingSequenceRemoved = false;
        sequenceStore.RemoveAsync("refusing").Returns(_ =>
        {
            isRefusingSequenceRemoved = true;
            return ValueTask.CompletedTask;
        });

        // The reader returns the sequence found in the store, like the real readers do
        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(_ => ValueTask.FromResult(isRefusingSequenceRemoved ? newSequence : refusingSequence));

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        // Run on another thread, since the endless loop would block the test thread
        await Task.Run(() => behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask()).WaitAsync(Timeout);

        // Removed only after refusing the message twice (a single failure is a legitimate retry case, e.g. completed by its timeout)
        await refusingSequence.Received(2).AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
        await sequenceStore.Received(1).RemoveAsync("refusing");
        await newSequence.Received(1).AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
    }

    [Theory]
    [InlineData(true, SequenceAbortReason.ConsumerAborted)]
    [InlineData(false, SequenceAbortReason.Error)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortSequence_WhenProcessingIsCanceled(bool isConsumerStopping, SequenceAbortReason expectedReason)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<SequenceAbortReason> abortReasonTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ISequence sequence = CreateSequence("new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>()).Returns(call =>
        {
            abortReasonTaskCompletionSource.TrySetResult(call.Arg<SequenceAbortReason>());
            return Task.CompletedTask;
        });

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(sequence);

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.ProcessingTask = processingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        // The subscriber throws when the operation is canceled, either by the consumer stop or for another reason
        if (isConsumerStopping)
            await cancellationTokenSource.CancelAsync();

        processingTaskCompletionSource.SetException(new OperationCanceledException());

        SequenceAbortReason abortReason = await abortReasonTaskCompletionSource.Task.WaitAsync(Timeout);
        abortReason.ShouldBe(expectedReason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAddRefusedMessageToNewSequenceUnlessStopping_WhenRefusingSequenceIsCommittedOnAbort(bool isStopping)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();

        if (isStopping)
            await cancellationTokenSource.CancelAsync();

        // The sequence refused the message because of its timeout and was then committed on abort (e.g. skipped)
        TaskCompletionSource abortTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        abortTaskCompletionSource.SetResult();
        ISequence refusingSequence = CreateSequence("refusing", new AddToSequenceResult(false, AbortTask: abortTaskCompletionSource.Task));
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        int getSequenceCount = 0;

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context)
            .Returns(_ => ValueTask.FromResult(Interlocked.Increment(ref getSequenceCount) == 1 ? refusingSequence : newSequence));

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        // When stopping, the message is left out (and consumed again after the restart), like when refused by an aborted sequence
        await newSequence.Received(isStopping ? 0 : 1).AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
        context.Sequence.ShouldBe(isStopping ? refusingSequence : newSequence);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotPublishNewSequence_WhenConsumerIsStopping()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();

        // Stopped while the new sequence was waiting for the previous ones (e.g. by the error policies of the previous batch)
        await cancellationTokenSource.CancelAsync();
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        newSequence.IsNew.Returns(true);

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(newSequence);

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());
        bool isPublished = false;

        // The stop cancellation rolls back the message, without applying the error policies
        await Should.ThrowAsync<OperationCanceledException>(() => behavior.HandleAsync(
                context,
                (_, _) =>
                {
                    isPublished = true;
                    return ValueTask.CompletedTask;
                },
                cancellationTokenSource.Token)
            .AsTask()
            .WaitAsync(Timeout));

        isPublished.ShouldBeFalse();
        await newSequence.DidNotReceive().AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
        await newSequence.Received(1).AbortAsync(SequenceAbortReason.ConsumerAborted, Arg.Any<OperationCanceledException>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortPreviousSequence_WhenConsumerIsStoppingBeforeNewSequenceIsPublished()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();

        // The chunk sequence of a chunked message, to be added to the batch it starts
        ISequence previousSequence = CreateSequence("previous", AddToSequenceResult.Success(1));
        context.SetSequence(previousSequence, true);

        await cancellationTokenSource.CancelAsync();
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        newSequence.IsNew.Returns(true);

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(newSequence);

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await Should.ThrowAsync<OperationCanceledException>(() => behavior.HandleAsync(
                context,
                (_, _) => ValueTask.CompletedTask,
                cancellationTokenSource.Token)
            .AsTask()
            .WaitAsync(Timeout));

        // The previous sequence isn't added to the aborted new sequence: it must be aborted as well, not to be left in the store forever
        await previousSequence.Received(1).AbortAsync(SequenceAbortReason.ConsumerAborted, Arg.Any<OperationCanceledException>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortPreviousSequenceOnlyWhenStopping_WhenRefusingSequenceIsCommittedOnAbort(bool isStopping)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();

        // The chunk sequence of a chunked message, to be added to the batch
        ISequence previousSequence = CreateSequence("previous", AddToSequenceResult.Success(1));
        context.SetSequence(previousSequence, true);

        if (isStopping)
            await cancellationTokenSource.CancelAsync();

        TaskCompletionSource abortTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        abortTaskCompletionSource.SetResult();
        ISequence refusingSequence = CreateSequence("refusing", new AddToSequenceResult(false, AbortTask: abortTaskCompletionSource.Task));
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        int getSequenceCount = 0;

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context)
            .Returns(_ => ValueTask.FromResult(Interlocked.Increment(ref getSequenceCount) == 1 ? refusingSequence : newSequence));

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        // When stopping, the message is left out and its previous sequence must be aborted, otherwise it's added to the new sequence
        await previousSequence.Received(isStopping ? 1 : 0)
            .AbortAsync(SequenceAbortReason.ConsumerAborted, Arg.Any<OperationCanceledException>());
        await newSequence.Received(isStopping ? 0 : 1).AddAsync(Arg.Any<IRawInboundEnvelope>(), previousSequence, Arg.Any<bool>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortPreviousSequenceUnlessAdded_WhenRefusedByAbortedSequence(bool isPreviousSequenceAdded)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();

        // The chunk sequence of a chunked message, to be added to the batch
        ISequence previousSequence = CreateSequence("previous", AddToSequenceResult.Success(1));
        context.SetSequence(previousSequence, true);

        // The batch refused the message (e.g. because of the timeout) and was then rolled back (e.g. retried)
        TaskCompletionSource abortTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        abortTaskCompletionSource.SetResult();
        ISequence abortedSequence = CreateSequence("aborted", AddToSequenceResult.Aborted(abortTaskCompletionSource.Task));

        if (isPreviousSequenceAdded)
            previousSequence.ParentSequence.Returns(abortedSequence);

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(abortedSequence);

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The previous sequence left out of the aborted one is rolled back as well, not to be left in the store forever, while the one
        // added to it is completed by its abort
        await previousSequence.Received(isPreviousSequenceAdded ? 0 : 1)
            .AbortAsync(SequenceAbortReason.ConsumerAborted, Arg.Any<Exception?>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task HandleAsync_ShouldAddMessageToNewSequence_WhenBatchIsCompletedAfterBeingFound()
    {
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10
            }
        };
        InboundEnvelope envelope = new(
            "message",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);

        // The reader found the batch pending, but it was completed (e.g. by its max wait time) right afterwards
        using CompletableBatchSequence completedBatch = new("batch-1", context);
        await completedBatch.CompleteFromTestAsync();
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        int getSequenceCount = 0;

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context)
            .Returns(_ => ValueTask.FromResult(Interlocked.Increment(ref getSequenceCount) == 1 ? completedBatch : newSequence));

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The message isn't skipped (and committed without being processed) but added to the next batch
        await newSequence.Received(1).AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
        context.Sequence.ShouldBe(newSequence);
    }

    private static ISequence CreateSequence(string sequenceId, AddToSequenceResult addToSequenceResult)
    {
        ISequence sequence = Substitute.For<ISequence>();
        sequence.SequenceId.Returns(sequenceId);
        sequence.IsNew.Returns(false);
        sequence.IsPending.Returns(true);
        sequence.ParentSequence.Returns((ISequence?)null);
        sequence.AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>()).Returns(addToSequenceResult);
        return sequence;
    }

    private sealed class CompletableBatchSequence : BatchSequence
    {
        public CompletableBatchSequence(string sequenceId, ConsumerPipelineContext context)
            : base(sequenceId, context)
        {
        }

        public Task CompleteFromTestAsync() => CompleteAsync();
    }
}
