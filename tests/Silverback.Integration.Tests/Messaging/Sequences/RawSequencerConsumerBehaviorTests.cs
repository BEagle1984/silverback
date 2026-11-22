// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class RawSequencerConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void SortIndex_ShouldReturnRawSequencerSortIndex()
    {
        RawSequencerConsumerBehavior behavior = new([], Substitute.For<ISilverbackLogger<RawSequencerConsumerBehavior>>());

        behavior.SortIndex.ShouldBe(BrokerBehaviorsSortIndexes.Consumer.RawSequencer);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldCallNextAndIgnoreNonRawSequenceReaders()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: sequenceStore);

        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.HandlesRawMessages.Returns(false);
        sequenceReader.CanHandleAsync(Arg.Any<ConsumerPipelineContext>()).Returns(true);

        RawSequencerConsumerBehavior behavior = new([sequenceReader], Substitute.For<ISilverbackLogger<RawSequencerConsumerBehavior>>());
        ConsumerPipelineContext? nextContext = null;

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                nextContext = pipelineContext;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // Unlike the SequencerConsumerBehavior, the pending sequences aren't aborted (the deserialized message may still belong to one)
        nextContext.ShouldBeSameAs(context);
        await sequenceReader.DidNotReceive().CanHandleAsync(Arg.Any<ConsumerPipelineContext>());
        sequenceStore.DidNotReceive().GetPendingSequences(Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldPublishNewSequenceWithoutAwaitingProcessing()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource nextCalledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNextTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ISequence sequence = CreateSequence();
        sequence.IsNew.Returns(true);

        RawSequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<RawSequencerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            async (_, _) =>
            {
                nextCalledTaskCompletionSource.TrySetResult();
                await releaseNextTaskCompletionSource.Task;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The next behaviors (and the subscribers) process the sequence in the background, while the next messages are added to it
        await nextCalledTaskCompletionSource.Task.WaitAsync(Timeout);
        Task processingTask = context.ProcessingTask.ShouldNotBeNull();
        processingTask.IsCompleted.ShouldBeFalse();
        await sequence.Received(1).AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>());

        releaseNextTaskCompletionSource.SetResult();
        await processingTask.WaitAsync(Timeout);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldKeepProcessingTask_WhenAlreadySet()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource nextCalledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource existingProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.ProcessingTask = existingProcessingTaskCompletionSource.Task;

        ISequence sequence = CreateSequence();
        sequence.IsNew.Returns(true);

        RawSequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<RawSequencerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (_, _) =>
            {
                nextCalledTaskCompletionSource.TrySetResult();
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        await nextCalledTaskCompletionSource.Task.WaitAsync(Timeout);
        context.ProcessingTask.ShouldBeSameAs(existingProcessingTaskCompletionSource.Task);

        existingProcessingTaskCompletionSource.SetResult();
    }

    [Theory]
    [InlineData(TaskStatus.RanToCompletion)]
    [InlineData(TaskStatus.Canceled)]
    [InlineData(TaskStatus.Faulted)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldAwaitSequencerConsumerBehavior_WhenSequenceIsCompleted(TaskStatus sequencerBehaviorsTaskStatus)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        TaskCompletionSource sequencerBehaviorsTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // The message completes the sequence: still pending when found by the reader, complete after being added
        SubstitutableSequence sequence = Substitute.For<SubstitutableSequence>();
        ConfigureSequence(sequence);
        sequence.IsComplete.Returns(false, true);
        sequence.Context.Returns(context);
        sequence.SequencerBehaviorsTask.Returns(sequencerBehaviorsTaskCompletionSource.Task);

        RawSequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<RawSequencerConsumerBehavior>>());

        Task handleTask = behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask();

        // The sequence end can be determined only after the SequencerConsumerBehavior processed the message (e.g. chunk -> batch)
        handleTask.IsCompleted.ShouldBeFalse();
        context.IsSequenceEnd.ShouldBeFalse();

        switch (sequencerBehaviorsTaskStatus)
        {
            case TaskStatus.Canceled:
                sequencerBehaviorsTaskCompletionSource.SetCanceled();
                break;
            case TaskStatus.Faulted:
                sequencerBehaviorsTaskCompletionSource.SetException(new InvalidOperationException("test"));
                break;
            default:
                sequencerBehaviorsTaskCompletionSource.SetResult();
                break;
        }

        // The errors are handled monitoring the processing task instead
        await handleTask.WaitAsync(Timeout);
        context.IsSequenceEnd.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldMarkSequenceEnd_WhenSequenceIsCompleted()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();

        ISequence sequence = CreateSequence();
        sequence.IsComplete.Returns(false, true);
        sequence.Context.Returns(context);

        RawSequencerConsumerBehavior behavior = new(
            [CreateSequenceReader(context, sequence)],
            Substitute.For<ISilverbackLogger<RawSequencerConsumerBehavior>>());

        await behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask().WaitAsync(Timeout);

        context.IsSequenceEnd.ShouldBeTrue();
    }

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    private static ISequenceReader CreateSequenceReader(ConsumerPipelineContext context, ISequence sequence)
    {
        ISequenceReader sequenceReader = Substitute.For<ISequenceReader>();
        sequenceReader.HandlesRawMessages.Returns(true);
        sequenceReader.CanHandleAsync(context).Returns(true);
        sequenceReader.GetSequenceAsync(context).Returns(sequence);
        return sequenceReader;
    }

    private static ISequence CreateSequence()
    {
        ISequence sequence = Substitute.For<ISequence>();
        ConfigureSequence(sequence);
        return sequence;
    }

    private static void ConfigureSequence(ISequence sequence)
    {
        sequence.SequenceId.Returns("sequence");
        sequence.IsNew.Returns(false);
        sequence.IsPending.Returns(true);
        sequence.ParentSequence.Returns((ISequence?)null);
        sequence.AddAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<ISequence?>(), Arg.Any<bool>()).Returns(AddToSequenceResult.Success(1));
    }
}
