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
        await Task.Run(() => behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, default).AsTask()).WaitAsync(Timeout);

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
}
