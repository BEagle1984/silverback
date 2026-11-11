// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequenceBaseTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task AbortAsync_ShouldAwaitPendingAbort_WhenAlreadyAborting()
    {
        TaskCompletionSource removeTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource removeCalledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        sequenceStore.RemoveAsync(Arg.Any<string>()).Returns(_ =>
        {
            removeCalledTaskCompletionSource.TrySetResult();
            return new ValueTask(removeTaskCompletionSource.Task);
        });
        using TestSequence sequence = CreateSequence(new HookLogger(), sequenceStore);

        Task firstAbortTask = sequence.AbortAsync(SequenceAbortReason.ConsumerAborted);
        await removeCalledTaskCompletionSource.Task.WaitAsync(Timeout);
        Task secondAbortTask = sequence.AbortAsync(SequenceAbortReason.ConsumerAborted);
        await Task.WhenAny(secondAbortTask, Task.Delay(100));
        bool isCompletedBeforeFirstAbort = secondAbortTask.IsCompleted;

        // Release the first abort before asserting, otherwise a failure would block the disposal forever
        removeTaskCompletionSource.SetResult();
        await Task.WhenAll(firstAbortTask, secondAbortTask).WaitAsync(Timeout);

        isCompletedBeforeFirstAbort.ShouldBeFalse();
        sequence.IsAborted.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task AbortIfIncompleteAsync_ShouldAwaitPendingAbort_WhenAlreadyAborting()
    {
        TaskCompletionSource removeTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource removeCalledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        sequenceStore.RemoveAsync(Arg.Any<string>()).Returns(_ =>
        {
            removeCalledTaskCompletionSource.TrySetResult();
            return new ValueTask(removeTaskCompletionSource.Task);
        });
        using TestSequence sequence = CreateSequence(new HookLogger(), sequenceStore);

        Task abortTask = sequence.AbortAsync(SequenceAbortReason.ConsumerAborted);
        await removeCalledTaskCompletionSource.Task.WaitAsync(Timeout);
        Task abortIfIncompleteTask = sequence.AbortIfIncompleteAsync();
        await Task.WhenAny(abortIfIncompleteTask, Task.Delay(100));
        bool isCompletedBeforeAbort = abortIfIncompleteTask.IsCompleted;

        // Release the abort before asserting, otherwise a failure would block the disposal forever
        removeTaskCompletionSource.SetResult();
        await Task.WhenAll(abortTask, abortIfIncompleteTask).WaitAsync(Timeout);

        isCompletedBeforeAbort.ShouldBeFalse();
        sequence.IsAborted.ShouldBeTrue();
        sequence.AbortReason.ShouldBe(SequenceAbortReason.ConsumerAborted);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task Dispose_ShouldNotBlock_WhenAbortFailed()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        sequenceStore.RemoveAsync(Arg.Any<string>()).Returns(ValueTask.FromException(new InvalidOperationException("test")));
        TestSequence sequence = CreateSequence(new HookLogger(), sequenceStore);

        await Should.ThrowAsync<InvalidOperationException>(() => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted));

        await Task.Run(sequence.Dispose).WaitAsync(Timeout);

        sequence.IsAborted.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hook and the tasks complete before the disposal")]
    public async Task AddAsync_ShouldNotDeadlock_WhenAbortedWhileCompletingSequence()
    {
        HookLogger logger = new();
        TestSequence sequence = CreateSequence(logger);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        using ManualResetEventSlim abortHoldsCompleteSemaphore = new();
        TaskCompletionSource addIsCompletingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Pause the AddAsync while it holds the add semaphore and is about to acquire the complete semaphore, until the
        // abort acquired the complete semaphore
        logger.OnLog(
            "{SequenceType} '{SequenceId}' is completing (total length {SequenceLength})...",
            () =>
            {
                addIsCompletingTaskCompletionSource.SetResult();
                abortHoldsCompleteSemaphore.Wait(Timeout);
            });

        _ = Task.Run(async () =>
        {
            await foreach (IRawInboundEnvelope dummy in stream)
            {
                // Just consume the stream
            }
        });
        Task<AddToSequenceResult> addTask = Task.Run(() => sequence.AddAsync(CreateEnvelope(), null, true));
        await addIsCompletingTaskCompletionSource.Task.WaitAsync(Timeout);

        logger.OnLog("Semaphore acquired", abortHoldsCompleteSemaphore.Set);
        Task abortTask = Task.Run(() => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted));

        await Task.WhenAll(addTask, abortTask).WaitAsync(Timeout);

        AddToSequenceResult addResult = await addTask;
        addResult.IsSuccess.ShouldBeTrue();
        sequence.IsAborted.ShouldBeTrue();

        // Dispose only if not deadlocked, otherwise it would block the test forever
        sequence.Dispose();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hook completes before the disposal")]
    public async Task Dispose_ShouldAwaitAbort_WhenAbortAcquiresCompleteSemaphoreBeforeDispose()
    {
        HookLogger logger = new();
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        TestSequence sequence = CreateSequence(logger, sequenceStore);
        using ManualResetEventSlim disposeHoldsAddSemaphore = new();
        TaskCompletionSource abortHoldsCompleteSemaphoreTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Pause the abort while it holds the complete semaphore and before it starts aborting, until the dispose acquired the add
        // semaphore
        logger.OnLog(
            "Semaphore acquired",
            () =>
            {
                abortHoldsCompleteSemaphoreTaskCompletionSource.SetResult();
                disposeHoldsAddSemaphore.Wait(Timeout);
            });
        logger.OnLog("Dispose is waiting complete semaphore ({SequenceType} '{SequenceId}')...", disposeHoldsAddSemaphore.Set);

        Task abortTask = Task.Run(() => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted));
        await abortHoldsCompleteSemaphoreTaskCompletionSource.Task.WaitAsync(Timeout);
        Task disposeTask = Task.Run(sequence.Dispose);

        await Task.WhenAll(abortTask, disposeTask).WaitAsync(Timeout);

        sequence.IsAborted.ShouldBeTrue();
        await sequenceStore.Received(1).RemoveAsync(sequence.SequenceId);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hooks complete before the disposal")]
    public async Task Dispose_ShouldStopPendingAdd_WhenAddStartedBeforeDisposing()
    {
        HookLogger logger = new();
        TestSequence sequence = CreateSequence(logger);
        using ManualResetEventSlim disposeHoldsAddSemaphore = new();
        using ManualResetEventSlim addIsWaiting = new();
        Task? disposeTask = null;

        // Pause the dispose while it holds the add semaphore and before it stops the pending waits, until the add is waiting for the
        // add semaphore
        logger.OnLog(
            "Dispose is waiting complete semaphore ({SequenceType} '{SequenceId}')...",
            () =>
            {
                disposeHoldsAddSemaphore.Set();
                addIsWaiting.Wait(Timeout);
            });

        // Dispose after the add checked whether the sequence is disposed and before it waits for the add semaphore
        logger.OnLog(
            "Waiting for semaphore...",
            () =>
            {
                disposeTask = Task.Run(sequence.Dispose);
                disposeHoldsAddSemaphore.Wait(Timeout);
            });

        // AddAsync returns the pending task as soon as it is waiting for the add semaphore
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), null, true);
        bool isAddCompletedBeforeDisposeContinues = addTask.IsCompleted;
        addIsWaiting.Set();

        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);
        await disposeTask.ShouldNotBeNull().WaitAsync(Timeout);

        isAddCompletedBeforeDisposeContinues.ShouldBeFalse();
        addResult.ShouldBe(AddToSequenceResult.Failed);
        sequence.IsPending.ShouldBeTrue();
    }

    [Theory]
    [InlineData(nameof(ISequence.AbortAsync))]
    [InlineData(nameof(ISequence.AbortIfIncompleteAsync))]
    [InlineData(nameof(TestSequence.CompleteFromTestAsync))]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hooks complete before the disposal")]
    public async Task Dispose_ShouldStopPendingCompleteOrAbort_WhenStartedBeforeDisposing(string operation)
    {
        HookLogger logger = new();
        TestSequence sequence = CreateSequence(logger);
        using ManualResetEventSlim disposeHoldsSemaphores = new();
        using ManualResetEventSlim operationStarted = new();
        Task? disposeTask = null;

        // Pause the dispose while it holds the semaphores and before it disposes them (when it disposes the child sequences), until
        // the operation started waiting for the complete semaphore. Nothing is logged between acquiring the complete semaphore and
        // stopping the pending waits, so the operation starts waiting after the waits are stopped.
        ISequence childSequence = Substitute.For<ISequence>();
        childSequence.When(child => child.Dispose()).Do(
            _ =>
            {
                disposeHoldsSemaphores.Set();
                operationStarted.Wait(Timeout);
            });
        await sequence.AddAsync(CreateEnvelope(), childSequence, false).WaitAsync(Timeout);

        // Dispose after the operation checked whether the sequence is disposed and before it waits for the complete semaphore
        logger.OnLog(
            "Waiting for semaphore...",
            () =>
            {
                disposeTask = Task.Run(sequence.Dispose);
                disposeHoldsSemaphores.Wait(Timeout);
            });

        Task operationTask = operation switch
        {
            nameof(ISequence.AbortAsync) => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted),
            nameof(ISequence.AbortIfIncompleteAsync) => sequence.AbortIfIncompleteAsync(),
            nameof(TestSequence.CompleteFromTestAsync) => sequence.CompleteFromTestAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
        operationStarted.Set();

        await operationTask.WaitAsync(Timeout);
        await disposeTask.ShouldNotBeNull().WaitAsync(Timeout);

        sequence.IsPending.ShouldBeTrue();
    }

    [Fact]
    public async Task CompleteAsync_ShouldAbort_WhenProcessingAlreadyCompleted()
    {
        TestSequence sequence = CreateSequence(new HookLogger());
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        await using IAsyncEnumerator<IRawInboundEnvelope> enumerator = stream.GetAsyncEnumerator();
        sequence.Context.ProcessingTask = Task.CompletedTask;

        // Pull the message and stop enumerating: its push waits for the next pull, which never comes (and, being the first
        // message, nothing but the sequencer monitor is watching the processing task yet)
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), null, true);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();

        await sequence.CompleteFromTestAsync().WaitAsync(Timeout);
        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);

        sequence.IsAborted.ShouldBeTrue();
        sequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);
        sequence.IsCompleting.ShouldBeFalse();
        addResult.IsAborted.ShouldBeTrue();
        sequence.Dispose();
    }

    [Fact]
    public async Task CompleteAsync_ShouldNotSetCompleting_WhenAlreadyAborted()
    {
        TestSequence sequence = CreateSequence(new HookLogger());
        await sequence.AbortAsync(SequenceAbortReason.ConsumerAborted).WaitAsync(Timeout);

        await sequence.CompleteFromTestAsync().WaitAsync(Timeout);

        sequence.IsCompleting.ShouldBeFalse();
        sequence.IsAborted.ShouldBeTrue();
        sequence.Dispose();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The hook completes before the disposal")]
    public async Task CompleteAsync_ShouldNotBlock_WhenProcessingCompletesWhileCompleting()
    {
        HookLogger logger = new();
        TestSequence sequence = CreateSequence(logger);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        await using IAsyncEnumerator<IRawInboundEnvelope> enumerator = stream.GetAsyncEnumerator();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        sequence.Context.ProcessingTask = processingTaskCompletionSource.Task;
        Task? earlyExitHandlingTask = null;

        // Pull the message and stop enumerating: its push waits for the next pull, which never comes
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), null, true);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();

        // Complete the processing while the completion is starting and handle the early exit as the TransactionHandlerConsumerBehavior
        // does: abort unless completing, otherwise commit and dispose (which cancels the pending push)
        logger.OnLog(
            "Completing {SequenceType} '{SequenceId}' (length {SequenceLength})...",
            () =>
            {
                processingTaskCompletionSource.SetResult();

                earlyExitHandlingTask = sequence.IsCompleting || sequence.IsComplete
                    ? Task.Run(
                        () =>
                        {
                            ((ISequenceImplementation)sequence).NotifyProcessingCompleted();
                            sequence.Dispose();
                        })
                    : Task.Run(() => sequence.AbortAsync(SequenceAbortReason.EnumerationAborted));
            });

        await sequence.CompleteFromTestAsync().WaitAsync(Timeout);
        await earlyExitHandlingTask.ShouldNotBeNull().WaitAsync(Timeout);
        await addTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task AddAsync_ShouldCompleteSequence_WhenLastMessageIsAddedAfterProcessingCompleted()
    {
        // E.g., a chunk sequence in a batch, whose processing task completes when the batch accepts the message
        TestSequence sequence = CreateSequence(new HookLogger());
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        sequence.Context.ProcessingTask = Task.CompletedTask;

        Task<int> enumerationTask = Task.Run(async () =>
        {
            int count = 0;

            await foreach (IRawInboundEnvelope dummy in stream)
            {
                count++;
            }

            return count;
        });
        AddToSequenceResult addResult = await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);

        int enumeratedCount = await enumerationTask.WaitAsync(Timeout);

        addResult.IsSuccess.ShouldBeTrue();
        sequence.IsComplete.ShouldBeTrue();
        enumeratedCount.ShouldBe(1);
        sequence.Dispose();
    }

    [Fact]
    public async Task IsLastPulled_ShouldReturnFalse_WhenAbortedBeforeMessageIsLastPulled()
    {
        TestSequence sequence = CreateSequence(new HookLogger());
        sequence.CreateStream<IRawInboundEnvelope>();
        RawInboundEnvelope envelope = CreateEnvelope();

        Task<AddToSequenceResult> addTask = sequence.AddAsync(envelope, null, true);
        await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).WaitAsync(Timeout);
        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);

        addResult.IsAborted.ShouldBeTrue();
        sequence.IsLastPulled(envelope).ShouldBeFalse();
        sequence.Dispose();
    }

    [Fact]
    public async Task IsLastPulled_ShouldReturnTrue_WhenAbortedAfterMessageIsLastPulled()
    {
        TestSequence sequence = CreateSequence(new HookLogger());
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        await using IAsyncEnumerator<IRawInboundEnvelope> enumerator = stream.GetAsyncEnumerator();
        RawInboundEnvelope envelope = CreateEnvelope();

        Task<AddToSequenceResult> addTask = sequence.AddAsync(envelope, null, true);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();
        await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).WaitAsync(Timeout);
        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);

        addResult.IsAborted.ShouldBeTrue();
        sequence.IsLastPulled(envelope).ShouldBeTrue();
        sequence.IsLastPulled(CreateEnvelope()).ShouldBeFalse();
        sequence.Dispose();
    }

    [Fact]
    public async Task IsLastPulled_ShouldReturnFalse_WhenAddedToAbortedSequence()
    {
        TestSequence sequence = CreateSequence(new HookLogger());
        sequence.CreateStream<IRawInboundEnvelope>();
        await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).WaitAsync(Timeout);
        RawInboundEnvelope envelope = CreateEnvelope();

        AddToSequenceResult addResult = await sequence.AddAsync(envelope, null, true).WaitAsync(Timeout);

        addResult.IsAborted.ShouldBeTrue();
        sequence.IsLastPulled(envelope).ShouldBeFalse();
        sequence.Dispose();
    }

    [Theory]
    [InlineData("skip")]
    [InlineData("enumeration-aborted")]
    public async Task AddAsync_ShouldFailWithoutAborting_WhenRefusedByTimeoutAndSequenceIsCommittedOnAbort(string abort)
    {
        AddToSequenceResult result = await AddRefusedByTimeoutAsync(abort);

        // The message didn't join the committed sequence: the sequencer adds it to a new sequence, unless stopping (see AbortTask)
        result.IsSuccess.ShouldBeFalse();
        result.IsAborted.ShouldBeFalse();
        (result.AbortTask?.IsCompleted).ShouldBe(true);
    }

    [Theory]
    [InlineData("retry")]
    [InlineData("incomplete")]
    [InlineData("consumer-aborted")]
    [InlineData("disposing")]
    public async Task AddAsync_ShouldReturnAborted_WhenRefusedByTimeoutAndSequenceIsNotCommittedOnAbort(string abort)
    {
        AddToSequenceResult result = await AddRefusedByTimeoutAsync(abort);

        // The message is consumed again after the rollback (or the restart), or discarded with the incomplete sequence
        result.IsAborted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("skip", false)]
    [InlineData("enumeration-aborted", false)]
    [InlineData("skip", true)]
    public async Task AddAsync_ShouldFailWithoutAborting_WhenAbortedAfterBeingFoundAndCommittedOnAbort(string abort, bool isDisposed)
    {
        AddToSequenceResult result = await AddToSequenceAbortedAfterBeingFoundAsync(abort, isDisposed);

        // The message isn't committed with the sequence (and then with the next message without being processed): the sequencer adds it
        // to a new sequence, unless stopping (see AbortTask)
        result.IsSuccess.ShouldBeFalse();
        result.IsAborted.ShouldBeFalse();
        (result.AbortTask?.IsCompleted).ShouldBe(true);
    }

    [Theory]
    [InlineData("retry")]
    [InlineData("incomplete")]
    [InlineData("consumer-aborted")]
    [InlineData("disposing")]
    public async Task AddAsync_ShouldReturnAborted_WhenAbortedAfterBeingFoundAndNotCommittedOnAbort(string abort)
    {
        AddToSequenceResult result = await AddToSequenceAbortedAfterBeingFoundAsync(abort, false);

        // The message is consumed again after the rollback (or the restart), or discarded with the incomplete sequence
        result.IsAborted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("skip", false)]
    [InlineData("enumeration-aborted", false)]
    [InlineData("skip", true)]
    public async Task AddAsync_ShouldReturnAborted_WhenSequenceStartedByMessageIsAbortedAndCommittedOnAbort(string abort, bool isDisposed)
    {
        AddToSequenceResult result = await AddToSequenceAbortedAfterBeingFoundAsync(abort, isDisposed, isFound: false);

        // The sequence shares the context of the message that started it, which can't be reused for a new sequence: the message shares
        // the outcome of the sequence instead (e.g. skipped or ignored with it)
        result.IsAborted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("enumeration-aborted", true)]
    [InlineData("consumer-aborted", true)]
    [InlineData("enumeration-aborted", false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by the add, before the disposal")]
    public async Task AddAsync_ShouldRefuseMessageAndSequence_WhenAbortedRightBeforePush(string abort, bool isFound)
    {
        using AbortTestSequence sequence = new(CreateErrorPolicyContext(new HookLogger(), new SkipMessageErrorPolicy()));

        // Found in the store by the reader, otherwise it's the new sequence started by the added message
        if (isFound)
            ((ISequenceImplementation)sequence).SetIsNew(false);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (IRawInboundEnvelope dummy in stream)
                {
                    // Just consume the stream
                }
            }
            catch (OperationCanceledException)
            {
                // The stream is aborted with the sequence
            }
        });

        AddToSequenceResult firstResult = await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);
        firstResult.IsSuccess.ShouldBeTrue();

        // Aborted after the checks, while the chunk sequence of the next (chunked) message is being linked, right before the push
        Task? abortTask = null;
        using HookChildSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            () => abortTask = sequence.AbortAsync(
                abort == "enumeration-aborted" ? SequenceAbortReason.EnumerationAborted : SequenceAbortReason.ConsumerAborted));

        AddToSequenceResult result = await sequence.AddAsync(CreateEnvelope(), childSequence, true).WaitAsync(Timeout);
        (abortTask != null).ShouldBeTrue();
        await abortTask!.WaitAsync(Timeout);

        // The message didn't join the sequence (instead of failing the processing), which doesn't commit or complete the chunk sequence.
        // The message that started the sequence shares its outcome instead of being added to the next one.
        childSequence.Parent.ShouldBeNull();
        sequence.Sequences.ShouldNotContain(childSequence);

        if (abort == "enumeration-aborted" && isFound)
        {
            result.IsSuccess.ShouldBeFalse();
            result.IsAborted.ShouldBeFalse();
            (result.AbortTask?.IsCompleted).ShouldBe(true);
        }
        else
        {
            result.IsAborted.ShouldBeTrue();
        }
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by the add, before the disposal")]
    public async Task AddAsync_ShouldUnlinkSequence_WhenStreamIsCompletedWhilePushing()
    {
        using AbortTestSequence sequence = new(CreateErrorPolicyContext(new HookLogger(), new SkipMessageErrorPolicy()));
        ((ISequenceImplementation)sequence).SetIsNew(false);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (IRawInboundEnvelope dummy in stream)
                {
                    // Just consume the stream
                }
            }
            catch (OperationCanceledException)
            {
                // The stream is aborted with the sequence
            }
        });

        AddToSequenceResult firstResult = await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);
        firstResult.IsSuccess.ShouldBeTrue();

        // The stream is completed (e.g. by the max wait time) while the chunk sequence of the next message is being linked, so that the
        // push is canceled
        using HookChildSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            () => _ = ((IMessageStreamEnumerable)stream).CompleteAsync());

        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), childSequence, true);
        ((ISequenceImplementation)sequence).NotifyProcessingCompleted();
        AddToSequenceResult result = await addTask.WaitAsync(Timeout);

        // The message is added to the next sequence, without leaving the chunk sequence linked to (and committed with) this one
        result.IsSuccess.ShouldBeFalse();
        result.IsAborted.ShouldBeFalse();
        childSequence.Parent.ShouldBeNull();
        sequence.Sequences.ShouldNotContain(childSequence);
    }

    private static async Task<AddToSequenceResult> AddRefusedByTimeoutAsync(string abort)
    {
        HookLogger logger = new();
        IErrorPolicy errorPolicy = abort == "retry" ? new RetryErrorPolicy { MaxFailedAttempts = 2 } : new SkipMessageErrorPolicy();
        using TimeoutTestSequence sequence = CreateTimeoutSequence(logger, errorPolicy);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (IRawInboundEnvelope dummy in stream)
                {
                    // Just consume the stream
                }
            }
            catch (OperationCanceledException)
            {
                // The stream is aborted with the sequence
            }
        });

        AddToSequenceResult firstResult = await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);
        firstResult.IsSuccess.ShouldBeTrue();

        // The timer is held while handling the timeout, so that the sequence is still pending when the next message is added
        await sequence.TimeoutElapsed.WaitAsync(Timeout);

        TaskCompletionSource addRefused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        logger.OnLog("Operation canceled, skipping add to {SequenceType} '{SequenceId}' and waiting until it is completed.", addRefused.SetResult);
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), null, true);
        await addRefused.Task.WaitAsync(Timeout);

        try
        {
            Task abortTask = abort switch
            {
                "skip" or "retry" => sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test")),
                "enumeration-aborted" => sequence.AbortAsync(SequenceAbortReason.EnumerationAborted),
                "incomplete" => sequence.AbortAsync(SequenceAbortReason.IncompleteSequence),
                "consumer-aborted" => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted),
                _ => sequence.AbortAsync(SequenceAbortReason.Disposing)
            };

            await abortTask.WaitAsync(Timeout);

            return await addTask.WaitAsync(Timeout);
        }
        finally
        {
            sequence.ReleaseTimeout();
        }
    }

    private static async Task<AddToSequenceResult> AddToSequenceAbortedAfterBeingFoundAsync(string abort, bool isDisposed, bool isFound = true)
    {
        IErrorPolicy errorPolicy = abort == "retry" ? new RetryErrorPolicy { MaxFailedAttempts = 2 } : new SkipMessageErrorPolicy();
        AbortTestSequence sequence = new(CreateErrorPolicyContext(new HookLogger(), errorPolicy));

        // Found in the store by the reader, otherwise it's the new sequence started by the added message
        if (isFound)
            ((ISequenceImplementation)sequence).SetIsNew(false);

        try
        {
            IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (IRawInboundEnvelope dummy in stream)
                    {
                        // Just consume the stream
                    }
                }
                catch (OperationCanceledException)
                {
                    // The stream is aborted with the sequence
                }
            });

            AddToSequenceResult firstResult = await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);
            firstResult.IsSuccess.ShouldBeTrue();

            // Aborted after the reader found it pending, before the next message is added
            Task abortTask = abort switch
            {
                "skip" or "retry" => sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test")),
                "enumeration-aborted" => sequence.AbortAsync(SequenceAbortReason.EnumerationAborted),
                "incomplete" => sequence.AbortAsync(SequenceAbortReason.IncompleteSequence),
                "consumer-aborted" => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted),
                _ => sequence.AbortAsync(SequenceAbortReason.Disposing)
            };
            await abortTask.WaitAsync(Timeout);

            if (isDisposed)
                sequence.Dispose();

            return await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);
        }
        finally
        {
            sequence.Dispose();
        }
    }

    private static TimeoutTestSequence CreateTimeoutSequence(HookLogger logger, IErrorPolicy errorPolicy) =>
        new(CreateErrorPolicyContext(logger, errorPolicy));

    private static ConsumerPipelineContext CreateErrorPolicyContext(HookLogger logger, IErrorPolicy errorPolicy)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker();

            services.AddSingleton<ISilverbackLogger<SequenceBase<IRawInboundEnvelope>>>(logger);
        });

        RawInboundEnvelope envelope = new(
            Stream.Null,
            [],
            new TestConsumerEndpointConfiguration("test") { ErrorPolicy = errorPolicy }.GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset());

        // A real transaction manager, tracking whether the error policy rolled back the consumer
        ConsumerTransactionManager transactionManager = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        return ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider, transactionManager);
    }

    private static TestSequence CreateSequence(HookLogger logger, ISequenceStore? sequenceStore = null)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker();

            services.AddSingleton<ISilverbackLogger<SequenceBase<IRawInboundEnvelope>>>(logger);
        });

        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            serviceProvider: serviceProvider,
            sequenceStore: sequenceStore);

        return new TestSequence(context);
    }

    private static RawInboundEnvelope CreateEnvelope() =>
        new(
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

    private sealed class TestSequence : SequenceBase<IRawInboundEnvelope>
    {
        public TestSequence(ConsumerPipelineContext context)
            : base("test-sequence", context, enforceTimeout: false, trackIdentifiers: false)
        {
            TotalLength = 1;
        }

        public Task CompleteFromTestAsync() => CompleteAsync();
    }

    // Adds the message refused because of the abort to the next sequence, like the batches
    private sealed class AbortTestSequence : SequenceBase<IRawInboundEnvelope>
    {
        public AbortTestSequence(ConsumerPipelineContext context)
            : base("abort-test-sequence", context, enforceTimeout: false, trackIdentifiers: false)
        {
        }

        private protected override bool IsMessageRefusedByAbortAddedToNextSequence => true;
    }

    // Runs the action when it's linked to the parent sequence, right before the message is pushed, to deterministically reproduce the
    // parent being aborted in between, and records the parent (re-implementing the interface hides the base implementation)
    private sealed class HookChildSequence : SequenceBase<IRawInboundEnvelope>, ISequenceImplementation
    {
        private readonly Action _onLinking;

        public HookChildSequence(ConsumerPipelineContext context, Action onLinking)
            : base("child-sequence", context, enforceTimeout: false, trackIdentifiers: false)
        {
            _onLinking = onLinking;
        }

        public ISequence? Parent { get; private set; }

        void ISequenceImplementation.SetParentSequence(ISequence? parentSequence)
        {
            if (parentSequence != null)
                _onLinking();

            Parent = parentSequence;
        }
    }

    // Holds the timer while handling the elapsed timeout, so that the sequence is still pending after its timeout elapsed
    private sealed class TimeoutTestSequence : SequenceBase<IRawInboundEnvelope>
    {
        private readonly TaskCompletionSource _timeoutElapsed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _releaseTimeout = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimeoutTestSequence(ConsumerPipelineContext context)
            : base("timeout-test-sequence", context, enforceTimeout: true, timeout: TimeSpan.FromMilliseconds(50), trackIdentifiers: false)
        {
        }

        public Task TimeoutElapsed => _timeoutElapsed.Task;

        public void ReleaseTimeout() => _releaseTimeout.TrySetResult();

        protected override Task OnTimeoutElapsedAsync()
        {
            _timeoutElapsed.TrySetResult();
            return _releaseTimeout.Task;
        }
    }

    // Runs the registered action (once) when the message with the specified template is logged, to deterministically reproduce
    // specific interleavings
    private sealed class HookLogger : ISilverbackLogger<SequenceBase<IRawInboundEnvelope>>, ILogger
    {
        private readonly ConcurrentDictionary<string, Action> _hooks = new();

        public ILogger InnerLogger => this;

        public void OnLog(string messageTemplate, Action action) => _hooks[messageTemplate] = action;

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
                return;

            if (values.FirstOrDefault(value => value.Key == "{OriginalFormat}").Value is string messageTemplate &&
                _hooks.TryRemove(messageTemplate, out Action? action))
            {
                action.Invoke();
            }
        }
    }
}
