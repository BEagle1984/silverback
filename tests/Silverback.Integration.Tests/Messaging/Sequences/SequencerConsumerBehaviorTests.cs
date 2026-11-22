// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequencerConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // The sequence set in the context of the completed sequence
    public enum SequenceContextState
    {
        NoSequence,

        SameSequence,

        CompletingSequence,

        CompleteSequence,

        PendingSequence
    }

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
    public async Task HandleAsync_ShouldNotPublishNewSequence_WhenReadingIsCanceled()
    {
        // The reading is canceled to roll back (or revoke) the partition, without stopping the consumer
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>(), () => true);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: sequenceStore);
        ISequence newSequence = CreateSequence("new", AddToSequenceResult.Success(1));
        newSequence.IsNew.Returns(true);

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(newSequence);

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());
        bool isPublished = false;

        // The message is rolled back and consumed again after the seek, instead of being processed by a sequence aborted afterwards
        await Should.ThrowAsync<OperationCanceledException>(() => behavior.HandleAsync(
                context,
                (_, _) =>
                {
                    isPublished = true;
                    return ValueTask.CompletedTask;
                },
                CancellationToken.None)
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

    [Fact]
    public void SortIndex_ShouldReturnSequencerSortIndex()
    {
        SequencerConsumerBehavior behavior = new([], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        behavior.SortIndex.ShouldBe(BrokerBehaviorsSortIndexes.Consumer.Sequencer);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldCallNextAndAbortPendingSequences_WhenNoReaderCanHandleMessage()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        ISequence pendingSequence1 = CreateSequence("pending-1", AddToSequenceResult.Success(1));
        ISequence pendingSequence2 = CreateSequence("pending-2", AddToSequenceResult.Success(1));
        sequenceStore.GetPendingSequences().Returns([pendingSequence1, pendingSequence2]);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: sequenceStore);

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(false);

        SequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());
        ConsumerPipelineContext? nextContext = null;

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                nextContext = pipelineContext;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // A message that doesn't belong to a sequence interrupts the pending sequences, which can't be completed anymore
        nextContext.ShouldBeSameAs(context);
        await sequenceReader.DidNotReceive().GetSequenceAsync(Arg.Any<ConsumerPipelineContext>());
        await pendingSequence1.Received(1).AbortAsync(SequenceAbortReason.IncompleteSequence);
        await pendingSequence2.Received(1).AbortAsync(SequenceAbortReason.IncompleteSequence);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldIgnoreRawSequenceReaders()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();

        ISequenceReader rawSequenceReader = Substitute.For<ISequenceReader>();
        rawSequenceReader.HandlesRawMessages.Returns(true);
        rawSequenceReader.CanHandleAsync(Arg.Any<ConsumerPipelineContext>()).Returns(true);

        SequencerConsumerBehavior behavior = new([rawSequenceReader], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());
        bool isNextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                isNextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        isNextCalled.ShouldBeTrue();
        await rawSequenceReader.DidNotReceive().CanHandleAsync(Arg.Any<ConsumerPipelineContext>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ShouldCompleteSequencerBehaviorsTaskOfRawSequence(bool isNextFailing)
    {
        // The chunk sequence set by the RawSequencerConsumerBehavior, which awaits the processing by this behavior
        SubstitutableSequence rawSequence = Substitute.For<SubstitutableSequence>();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequence: rawSequence);

        SequencerConsumerBehavior behavior = new([], Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        Task handleTask = behavior.HandleAsync(
            context,
            (_, _) => isNextFailing ? ValueTask.FromException(new InvalidOperationException("test")) : ValueTask.CompletedTask,
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        if (isNextFailing)
            await Should.ThrowAsync<InvalidOperationException>(handleTask);
        else
            await handleTask;

        rawSequence.Received(1).CompleteSequencerBehaviorsTask();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldSkipMessage_WhenSequenceIsAlreadyComplete()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();

        // The message belongs to a sequence that is complete already (e.g. a duplicated chunk)
        ISequence completeSequence = CreateSequence("complete", AddToSequenceResult.Success(1));
        completeSequence.IsComplete.Returns(true);

        SequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, completeSequence)],
            Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());
        bool isNextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                isNextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        isNextCalled.ShouldBeFalse();
        await completeSequence.DidNotReceive().AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
        context.Sequence.ShouldBeNull();
        context.IsSequenceEnd.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipMessageAndLog_WhenSequenceIsIncomplete()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();

        // The first message of the sequence is missing (e.g. a chunk other than the first one consumed after a rebalance)
        using IncompleteSequence incompleteSequence = new("incomplete", context);

        SilverbackLoggerSubstitute<SequencerConsumerBehavior> logger = new();
        SequencerConsumerBehavior behavior = new([CreateSequenceReader(context, incompleteSequence)], logger);
        bool isNextCalled = false;

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                isNextCalled = true;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        isNextCalled.ShouldBeFalse();
        context.Sequence.ShouldBeNull();
        logger.Received(LogLevel.Warning, null, "Skipped incomplete sequence incomplete (missing first message)", 1017);
    }

    [Fact]
    public async Task HandleAsync_ShouldTagCurrentActivityWithSequenceId()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ISequence sequence = CreateSequence("sequence-1", AddToSequenceResult.Success(1));

        SequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        using Activity activity = new Activity("test").Start();

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask().WaitAsync(Timeout);

        activity.GetTagItem(ActivityTagNames.SequenceId).ShouldBe("sequence-1");
    }

    [Theory]
    [InlineData(SequenceContextState.NoSequence, true)]
    [InlineData(SequenceContextState.SameSequence, true)]
    [InlineData(SequenceContextState.CompletingSequence, true)]
    [InlineData(SequenceContextState.CompleteSequence, true)]
    [InlineData(SequenceContextState.PendingSequence, false)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldMarkSequenceEndUnlessSwapped_WhenSequenceIsCompleted(
        SequenceContextState sequenceContextState,
        bool expectedIsSequenceEnd)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using ConsumerPipelineContext otherContext = ConsumerPipelineContextHelper.CreateSubstitute();

        // The message completes the sequence: still pending when found by the reader, complete after being added
        ISequence sequence = CreateSequence("sequence", AddToSequenceResult.Success(1));
        sequence.IsComplete.Returns(false, true);

        // The sequence of the context of the completed sequence differs if the completed sequence was swapped (e.g. chunk -> batch)
        ISequence otherSequence = CreateSequence("other", AddToSequenceResult.Success(1));
        otherSequence.IsCompleting.Returns(sequenceContextState == SequenceContextState.CompletingSequence);
        otherSequence.IsComplete.Returns(sequenceContextState == SequenceContextState.CompleteSequence);

        switch (sequenceContextState)
        {
            case SequenceContextState.NoSequence:
                sequence.Context.Returns(otherContext);
                break;
            case SequenceContextState.SameSequence:
                sequence.Context.Returns(context);
                break;
            default:
                otherContext.SetSequence(otherSequence, false);
                sequence.Context.Returns(otherContext);
                break;
        }

        SequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask().WaitAsync(Timeout);

        context.IsSequenceEnd.ShouldBe(expectedIsSequenceEnd);
    }

    [Theory]
    [InlineData(true, true, false, true, false, true)]
    [InlineData(true, true, false, false, true, true)]
    [InlineData(true, true, false, false, false, false)]
    [InlineData(false, true, false, true, false, false)]
    [InlineData(true, false, false, true, false, false)]
    [InlineData(true, true, true, true, false, false)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortSequenceOnlyIfPending_WhenPreviousSequenceIsLinkedWhileStopping(
        bool isPreviousSequencePending,
        bool isSequencePending,
        bool isSequenceCompleting,
        bool isStopping,
        bool isReadingCanceled,
        bool expectedIsAborted)
    {
        await using SequenceStore sequenceStore = new(new SilverbackLoggerSubstitute<SequenceStore>(), () => isReadingCanceled);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: sequenceStore);
        using CancellationTokenSource cancellationTokenSource = new();

        if (isStopping)
            await cancellationTokenSource.CancelAsync();

        // The pending batch, to which the chunk sequence of a chunked message is linked after the stop took its snapshot
        ISequence sequence = CreateSequence("batch", AddToSequenceResult.Success(1));
        sequence.IsPending.Returns(isSequencePending);
        sequence.IsCompleting.Returns(isSequenceCompleting);

        ISequence previousSequence = CreateSequence("chunks", AddToSequenceResult.Success(1));
        previousSequence.IsPending.Returns(isPreviousSequencePending);
        previousSequence.ParentSequence.Returns(sequence);
        context.SetSequence(previousSequence, true);

        SequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        // The stop would otherwise not abort the batch, while the reader may be blocked pushing the next chunk that nothing reads
        await sequence.Received(1).AddAsync(Arg.Any<IRawInboundEnvelope>(), previousSequence, Arg.Any<bool>());
        await sequence.Received(expectedIsAborted ? 1 : 0).AbortAsync(SequenceAbortReason.ConsumerAborted, Arg.Any<Exception?>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotAbortSequence_WhenPreviousSequenceIsSameSequence()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        // The sequence set in the context is the same sequence the message is added to, not a child sequence linked to it
        ISequence sequence = CreateSequence("sequence", AddToSequenceResult.Success(1));
        sequence.ParentSequence.Returns(sequence);
        context.SetSequence(sequence, false);

        SequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        await sequence.Received(1).AddAsync(Arg.Any<IRawInboundEnvelope>(), sequence, Arg.Any<bool>());
        await sequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortSequenceAsEnumerationAborted_WhenProcessingCompletesBeforeSequenceIsComplete()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<SequenceAbortReason> abortReasonTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ISequence sequence = CreateSequence("new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>()).Returns(call =>
        {
            abortReasonTaskCompletionSource.TrySetResult(call.Arg<SequenceAbortReason>());
            return Task.CompletedTask;
        });

        await HandleNewSequenceAsync(context, sequence, processingTaskCompletionSource.Task);

        // The subscriber returns without enumerating the whole sequence
        processingTaskCompletionSource.SetResult();

        // The abort releases the locks and commits the sequence
        SequenceAbortReason abortReason = await abortReasonTaskCompletionSource.Task.WaitAsync(Timeout);
        abortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAbortOnlyPendingChildSequences_WhenProcessingCompletesWithCompletedSequence(
        bool isCompleting,
        bool isComplete)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource childSequencesAbortedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // The sequence is completed (by its last message) by the time its subscriber returns
        SubstitutableSequence sequence = Substitute.For<SubstitutableSequence>();
        ConfigureSequence(sequence, "new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.IsCompleting.Returns(_ => isCompleting && processingTaskCompletionSource.Task.IsCompleted);
        sequence.IsComplete.Returns(_ => isComplete && processingTaskCompletionSource.Task.IsCompleted);
        sequence.AbortPendingChildSequencesAsync().Returns(_ =>
        {
            childSequencesAbortedTaskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        });

        await HandleNewSequenceAsync(context, sequence, processingTaskCompletionSource.Task);

        processingTaskCompletionSource.SetResult();

        // The child sequences still pending (e.g. a binary message not read to the end) would otherwise block the next push forever
        await childSequencesAbortedTaskCompletionSource.Task.WaitAsync(Timeout);
        await sequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotAbortSequence_WhenProcessingCompletesWithCompletedSequence()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource completionCheckedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ISequence sequence = CreateSequence("new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.IsCompleting.Returns(_ =>
        {
            if (!processingTaskCompletionSource.Task.IsCompleted)
                return false;

            completionCheckedTaskCompletionSource.TrySetResult();
            return true;
        });

        await HandleNewSequenceAsync(context, sequence, processingTaskCompletionSource.Task);

        processingTaskCompletionSource.SetResult();

        await completionCheckedTaskCompletionSource.Task.WaitAsync(Timeout);
        await sequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotAbortSequence_WhenProcessingCompletesAfterSequenceIsAddedToParentSequence()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource parentCheckedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISequence parentSequence = CreateSequence("parent", AddToSequenceResult.Success(1));

        // The chunk sequence is added to the batch after being published, and is then aborted (or completed) by the batch
        ISequence sequence = CreateSequence("new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.ParentSequence.Returns(_ =>
        {
            if (!processingTaskCompletionSource.Task.IsCompleted)
                return null;

            parentCheckedTaskCompletionSource.TrySetResult();
            return parentSequence;
        });

        await HandleNewSequenceAsync(context, sequence, processingTaskCompletionSource.Task);

        processingTaskCompletionSource.SetResult();

        await parentCheckedTaskCompletionSource.Task.WaitAsync(Timeout);
        await sequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotAbortSequence_WhenProcessingFailsAfterSequenceIsCompletedOrAddedToParentSequence(
        bool isPending,
        bool hasParentSequence)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stateCheckedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISequence parentSequence = CreateSequence("parent", AddToSequenceResult.Success(1));

        ISequence sequence = CreateSequence("new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.IsPending.Returns(_ =>
        {
            if (!processingTaskCompletionSource.Task.IsCompleted)
                return true;

            if (!isPending)
                stateCheckedTaskCompletionSource.TrySetResult();

            return isPending;
        });
        sequence.ParentSequence.Returns(_ =>
        {
            if (!processingTaskCompletionSource.Task.IsCompleted || !hasParentSequence)
                return null;

            stateCheckedTaskCompletionSource.TrySetResult();
            return parentSequence;
        });

        await HandleNewSequenceAsync(context, sequence, processingTaskCompletionSource.Task);

        // The error is handled with the sequence that is already complete (or with its parent sequence)
        processingTaskCompletionSource.SetException(new InvalidOperationException("test"));

        await stateCheckedTaskCompletionSource.Task.WaitAsync(Timeout);
        await sequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotAbortSequence_WhenProcessingOfChildSequenceFails()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ISequence parentSequence = CreateSequence("parent", AddToSequenceResult.Success(1));

        // A child sequence is aborted by its parent sequence instead
        ISequence sequence = CreateSequence("new", AddToSequenceResult.Success(1));
        sequence.IsNew.Returns(true);
        sequence.ParentSequence.Returns(parentSequence);

        await HandleNewSequenceAsync(context, sequence, Task.FromException(new InvalidOperationException("test")));

        await sequence.Received(1).AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());
        await sequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
    }

    private static Task HandleNewSequenceAsync(ConsumerPipelineContext context, ISequence sequence, Task processingTask)
    {
        SequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<SequencerConsumerBehavior>>());

        return behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.ProcessingTask = processingTask;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);
    }

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    private static ISequenceReader CreateSequenceReader(ConsumerPipelineContext context, ISequence sequence)
    {
        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(sequence);
        return sequenceReader;
    }

    private static ISequence CreateSequence(string sequenceId, AddToSequenceResult addToSequenceResult)
    {
        ISequence sequence = Substitute.For<ISequence>();
        ConfigureSequence(sequence, sequenceId, addToSequenceResult);
        return sequence;
    }

    private static void ConfigureSequence(ISequence sequence, string sequenceId, AddToSequenceResult addToSequenceResult)
    {
        sequence.SequenceId.Returns(sequenceId);
        sequence.IsNew.Returns(false);
        sequence.IsPending.Returns(true);
        sequence.ParentSequence.Returns((ISequence?)null);
        sequence.AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>()).Returns(addToSequenceResult);
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
