// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
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

    [Theory]
    [InlineData(SequenceAbortReason.Error)]
    [InlineData(SequenceAbortReason.EnumerationAborted)]
    [InlineData(SequenceAbortReason.IncompleteSequence)]
    public async Task AbortAsync_ShouldAbortPendingChildSequenceWithoutCompletingItsTransaction(SequenceAbortReason reason)
    {
        using AbortTestSequence parentSequence = new(
            CreateErrorPolicyContext(new HookLogger(), new SkipMessageErrorPolicy()),
            trackIdentifiers: true);
        ((ISequenceImplementation)parentSequence).SetIsNew(false);
        IConsumerTransactionManager childTransactionManager = Substitute.For<IConsumerTransactionManager>();
        ISequenceStore childSequenceStore = Substitute.For<ISequenceStore>();
        using TestSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(transactionManager: childTransactionManager, sequenceStore: childSequenceStore));
        Task<AddToSequenceResult> childAddTask = await AddPendingChildSequenceAsync(parentSequence, childSequence);

        await parentSequence
            .AbortAsync(reason, reason == SequenceAbortReason.Error ? new InvalidOperationException("test") : null)
            .WaitAsync(Timeout);

        // The child sequence shares the outcome of the parent, which completed the transaction, and the message being added is aborted
        // with it, instead of being added to a new sequence
        AddToSequenceResult childResult = await childAddTask.WaitAsync(Timeout);
        childResult.IsAborted.ShouldBeTrue();
        childSequence.IsAborted.ShouldBeTrue();
        childSequence.AbortReason.ShouldBe(reason);
        await childSequenceStore.Received(1).RemoveAsync(childSequence.SequenceId);
        childTransactionManager.ReceivedCalls().ShouldBeEmpty();
        await childSequence.ProcessingCompletedTask.WaitAsync(Timeout);
    }

    [Theory]
    [InlineData(SequenceAbortReason.ConsumerAborted)]
    [InlineData(SequenceAbortReason.Disposing)]
    public async Task AbortAsync_ShouldNotAbortPendingChildSequence_WhenConsumerAbortedOrDisposing(SequenceAbortReason reason)
    {
        using AbortTestSequence parentSequence = new(
            CreateErrorPolicyContext(new HookLogger(), new SkipMessageErrorPolicy()),
            trackIdentifiers: true);
        ((ISequenceImplementation)parentSequence).SetIsNew(false);
        IConsumerTransactionManager childTransactionManager = Substitute.For<IConsumerTransactionManager>();
        using TestSequence childSequence = new(ConsumerPipelineContextHelper.CreateSubstitute(transactionManager: childTransactionManager));
        Task<AddToSequenceResult> childAddTask = await AddPendingChildSequenceAsync(parentSequence, childSequence);

        await parentSequence.AbortAsync(reason).WaitAsync(Timeout);
        await Task.WhenAny(childAddTask, Task.Delay(100));

        // The stop aborts every pending sequence instead, child sequences included (see SequenceEnumerableExtensions.AbortAllAsync)
        childAddTask.IsCompleted.ShouldBeFalse();
        childSequence.IsPending.ShouldBeTrue();
        childSequence.AbortReason.ShouldBe(SequenceAbortReason.None);
        childTransactionManager.ReceivedCalls().ShouldBeEmpty();

        // Aborted on its own, the child sequence rolls back its own transaction
        await childSequence.AbortAsync(SequenceAbortReason.ConsumerAborted).WaitAsync(Timeout);

        AddToSequenceResult childResult = await childAddTask.WaitAsync(Timeout);
        childResult.IsAborted.ShouldBeTrue();
        childSequence.AbortReason.ShouldBe(SequenceAbortReason.ConsumerAborted);
        await childTransactionManager.Received(1).RollbackAsync(null, throwIfAlreadyCommitted: false, stopConsuming: false);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by the add, before the disposal")]
    public async Task AddAsync_ShouldUnlinkSequence_WhenStreamIsCompletedWhilePushing(bool isFound)
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

        // The sequence is being completed (e.g. by the max wait time) and its stream is completed while the chunk sequence of the next
        // message is being linked, so that the push is canceled before the streams provider is marked as completed
        using HookChildSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            () =>
            {
                typeof(SequenceBase<IRawInboundEnvelope>)
                    .GetField("_isCompleting", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(sequence, true);
                _ = ((IMessageStreamEnumerable)stream).CompleteAsync();
            });

        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), childSequence, true);
        ((ISequenceImplementation)sequence).NotifyProcessingCompleted();
        AddToSequenceResult result = await addTask.WaitAsync(Timeout);

        // The message is added to the next sequence (or shares the outcome of the sequence it started, never retried on its context),
        // without leaving the chunk sequence linked to (and committed with) this one
        result.IsSuccess.ShouldBe(!isFound);
        result.IsAborted.ShouldBe(!isFound);
        childSequence.Parent.ShouldBeNull();
        sequence.Sequences.ShouldNotContain(childSequence);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by the add, before the disposal")]
    public async Task AddAsync_ShouldRefuseMessage_WhenPushIsCanceledByCompletionAndSequenceIsThenSkipped()
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

        // The sequence is being completed (e.g. by the max wait time) and its stream completed, so that the push is canceled
        using HookChildSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            () =>
            {
                typeof(SequenceBase<IRawInboundEnvelope>)
                    .GetField("_isCompleting", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(sequence, true);
                _ = ((IMessageStreamEnumerable)stream).CompleteAsync();
            });

        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), childSequence, true);

        // The sequence then fails and is skipped
        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test")).WaitAsync(Timeout);
        AddToSequenceResult result = await addTask.WaitAsync(Timeout);

        // The message didn't join the sequence: it's added to the next one, instead of being skipped (and committed) with it
        result.IsSuccess.ShouldBeFalse();
        result.IsAborted.ShouldBeFalse();
        (result.AbortTask?.IsCompleted).ShouldBe(true);
        childSequence.Parent.ShouldBeNull();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The subscriber task is awaited before the disposal")]
    public async Task AddAsync_ShouldRefuseMessage_WhenEnumerationIsAbortedBeforeMessageIsPulled()
    {
        using AbortTestSequence sequence = new(
            CreateErrorPolicyContext(new HookLogger(), new SkipMessageErrorPolicy()),
            trackIdentifiers: true);
        ((ISequenceImplementation)sequence).SetIsNew(false);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TaskCompletionSource firstMessagePulled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource subscriberCancellationTokenSource = new();

        // The subscriber stops enumerating while waiting for the next message (e.g. canceled by its own token)
        Task subscriberTask = Task.Run(async () =>
        {
            try
            {
                await foreach (IRawInboundEnvelope dummy in stream.WithCancellation(subscriberCancellationTokenSource.Token))
                {
                    firstMessagePulled.TrySetResult();
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped enumerating
            }
        });

        RawInboundEnvelope firstEnvelope = CreateEnvelope();
        AddToSequenceResult firstResult = await sequence.AddAsync(firstEnvelope, null, true).WaitAsync(Timeout);
        firstResult.IsSuccess.ShouldBeTrue();
        await firstMessagePulled.Task.WaitAsync(Timeout);
        await subscriberCancellationTokenSource.CancelAsync();
        await subscriberTask.WaitAsync(Timeout);

        // The next message is pushed before the sequence is aborted because the subscriber stopped enumerating, but nobody pulls it
        RawInboundEnvelope secondEnvelope = CreateEnvelope();
        using HookChildSequence childSequence = new(ConsumerPipelineContextHelper.CreateSubstitute(), () => { });
        Task<AddToSequenceResult> addTask = sequence.AddAsync(secondEnvelope, childSequence, true);
        await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).WaitAsync(Timeout);
        AddToSequenceResult result = await addTask.WaitAsync(Timeout);

        // The message isn't committed with the sequence without being processed, but added to the next sequence
        result.IsSuccess.ShouldBeFalse();
        result.IsAborted.ShouldBeFalse();
        (result.AbortTask?.IsCompleted).ShouldBe(true);
        childSequence.Parent.ShouldBeNull();
        sequence.GetCommitIdentifiers().ShouldBe([firstEnvelope.BrokerMessageIdentifier]);
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("skipped")]
    [InlineData("retried")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The subscriber task is awaited before the disposal")]
    public async Task AddAsync_ShouldRefuseMessage_WhenSequenceIsCompletedAfterEnumerationIsAbortedBeforeMessageIsPulled(string outcome)
    {
        IErrorPolicy errorPolicy = outcome == "retried" ? new RetryErrorPolicy { MaxFailedAttempts = 2 } : new SkipMessageErrorPolicy();
        using AbortTestSequence sequence = new(CreateErrorPolicyContext(new HookLogger(), errorPolicy), trackIdentifiers: true);
        ((ISequenceImplementation)sequence).SetIsNew(false);
        RawInboundEnvelope secondEnvelope = CreateEnvelope();
        TaskCompletionSource secondMessagePushed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>(
        [
            new DelegateMessageFilter(message =>
            {
                if (ReferenceEquals(message, secondEnvelope))
                    secondMessagePushed.TrySetResult();

                return true;
            })
        ]);
        TaskCompletionSource firstMessagePulled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource subscriberCancellationTokenSource = new();

        // The subscriber stops enumerating while waiting for the next message (e.g. canceled by its own token)
        Task subscriberTask = Task.Run(async () =>
        {
            try
            {
                await foreach (IRawInboundEnvelope dummy in stream.WithCancellation(subscriberCancellationTokenSource.Token))
                {
                    firstMessagePulled.TrySetResult();
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped enumerating
            }
        });

        RawInboundEnvelope firstEnvelope = CreateEnvelope();
        AddToSequenceResult firstResult = await sequence.AddAsync(firstEnvelope, null, true).WaitAsync(Timeout);
        firstResult.IsSuccess.ShouldBeTrue();
        await firstMessagePulled.Task.WaitAsync(Timeout);
        await subscriberCancellationTokenSource.CancelAsync();
        await subscriberTask.WaitAsync(Timeout);

        // The next message is pushed, but nobody pulls it. The subscriber returned (abandoning the stream, see SubscribedMethodInvoker)
        // and the sequence is completed (e.g. by the max wait time) before the early exit handling aborts it, acknowledging the push.
        // The completion waits for the message being delivered to the stream, which the filter signals.
        using HookChildSequence childSequence = new(ConsumerPipelineContextHelper.CreateSubstitute(), () => { });
        Task<AddToSequenceResult> addTask = sequence.AddAsync(secondEnvelope, childSequence, true);
        await secondMessagePushed.Task.WaitAsync(Timeout);
        ((MessageStreamEnumerable<IRawInboundEnvelope>)stream).Abandon();
        await sequence.CompleteFromTestAsync().WaitAsync(Timeout);
        sequence.IsComplete.ShouldBeTrue();

        // Then the sequence is committed or, if the subscriber failed (rethrowing the cancellation), skipped or rolled back
        if (outcome == "completed")
            ((ISequenceImplementation)sequence).NotifyProcessingCompleted();
        else
            await sequence.AbortAsync(SequenceAbortReason.Error, new OperationCanceledException("test")).WaitAsync(Timeout);

        AddToSequenceResult result = await addTask.WaitAsync(Timeout);

        // The message isn't committed (or skipped) with the sequence without being processed, but added to the next sequence, unless the
        // sequence is rolled back, consuming the message again (an aborted result counts as handled, see AddToSequenceResult.Aborted)
        result.IsSuccess.ShouldBe(outcome == "retried");
        result.IsAborted.ShouldBe(outcome == "retried");
        childSequence.Parent.ShouldBeNull();
        sequence.Length.ShouldBe(1);
        sequence.GetCommitIdentifiers().ShouldBe([firstEnvelope.BrokerMessageIdentifier]);
    }

    [Fact]
    public async Task AddAsync_ShouldKeepSequenceLinked_WhenStreamsAreAbortedBeforeSequenceWhilePushing()
    {
        HookLogger logger = new();
        using AbortTestSequence sequence = new(CreateErrorPolicyContext(logger, new SkipMessageErrorPolicy()));
        ((ISequenceImplementation)sequence).SetIsNew(false);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TaskCompletionSource secondMessagePulled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSubscriber = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Task.Run(async () =>
        {
            int count = 0;

            try
            {
                await foreach (IRawInboundEnvelope dummy in stream)
                {
                    // Hold the second message without pulling the next one, so that its push stays pending
                    if (++count == 2)
                    {
                        secondMessagePulled.TrySetResult();
                        await releaseSubscriber.Task;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The stream is aborted with the sequence
            }
        });

        try
        {
            AddToSequenceResult firstResult = await sequence.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout);
            firstResult.IsSuccess.ShouldBeTrue();

            using HookChildSequence childSequence = new(ConsumerPipelineContextHelper.CreateSubstitute(), () => { });
            Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), childSequence, true);
            await secondMessagePulled.Task.WaitAsync(Timeout);

            // Another subscriber failed: the publisher aborts the streams first, and the sequence only once the subscribers returned
            TaskCompletionSource addCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            logger.OnLog(
                "Operation canceled, skipping add to {SequenceType} '{SequenceId}' and waiting until it is completed.",
                addCanceled.SetResult);
            ((MessageStreamProvider)sequence.StreamProvider).AbortIfPending();
            await addCanceled.Task.WaitAsync(Timeout);
            sequence.IsPending.ShouldBeTrue();
            await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test")).WaitAsync(Timeout);
            AddToSequenceResult result = await addTask.WaitAsync(Timeout);

            // The message (possibly processed by the healthy subscriber) shares the outcome of the abort with its chunk sequence
            result.IsAborted.ShouldBeTrue();
            childSequence.Parent.ShouldBe(sequence);
        }
        finally
        {
            releaseSubscriber.TrySetResult();
        }
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by the add, before the disposal")]
    public async Task AddAsync_ShouldRefuseMessage_WhenStreamsAreAbortedBeforeSequenceBeforePush()
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

        // Another subscriber failed right before the push: the publisher aborts the streams first, and the sequence only once the
        // subscribers returned
        using HookChildSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            () => ((MessageStreamProvider)sequence.StreamProvider).AbortIfPending());

        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(), childSequence, true);
        sequence.IsPending.ShouldBeTrue();
        await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test")).WaitAsync(Timeout);
        AddToSequenceResult result = await addTask.WaitAsync(Timeout);

        // The message didn't join the skipped sequence and is added to the next one, instead of failing the processing
        result.IsSuccess.ShouldBeFalse();
        result.IsAborted.ShouldBeFalse();
        (result.AbortTask?.IsCompleted).ShouldBe(true);
        childSequence.Parent.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldSkipFilteredMessage()
    {
        using TrackingTestSequence sequence = new(CreateContext(new HookLogger()), 2);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        Task<List<IRawInboundEnvelope>> enumerationTask = Task.Run(async () =>
        {
            List<IRawInboundEnvelope> envelopes = [];

            await foreach (IRawInboundEnvelope envelope in stream)
            {
                envelopes.Add(envelope);
            }

            return envelopes;
        });
        InboundEnvelope<TestEventOne> filteredEnvelope = CreateInboundEnvelope(true);
        InboundEnvelope<TestEventOne> envelope1 = CreateInboundEnvelope();
        InboundEnvelope<TestEventOne> envelope2 = CreateInboundEnvelope();

        AddToSequenceResult filteredResult = await sequence.AddAsync(filteredEnvelope, null, true).WaitAsync(Timeout);

        // Not pushed, counted or tracked, like a message that no stream accepts
        filteredResult.IsSuccess.ShouldBeTrue();
        filteredResult.PushedStreamsCount.ShouldBe(0);
        sequence.Length.ShouldBe(0);
        sequence.GetCommitIdentifiers().ShouldBeEmpty();

        await sequence.AddAsync(envelope1, null, true).WaitAsync(Timeout);
        await sequence.AddAsync(envelope2, null, true).WaitAsync(Timeout);
        List<IRawInboundEnvelope> enumeratedEnvelopes = await enumerationTask.WaitAsync(Timeout);

        sequence.IsComplete.ShouldBeTrue();
        sequence.Length.ShouldBe(2);
        enumeratedEnvelopes.ShouldBe([envelope1, envelope2]);
        sequence.GetCommitIdentifiers().ShouldBe([envelope1.BrokerMessageIdentifier, envelope2.BrokerMessageIdentifier], true);
    }

    [Fact]
    public async Task AddAsync_ShouldNotThrow_WhenFilteredMessageIsUnhandled()
    {
        using TrackingTestSequence sequence = new(CreateContext(new HookLogger()), 2);

        // No stream accepts the message, which would throw because of throwIfUnhandled if it wasn't filtered
        AddToSequenceResult result = await sequence.AddAsync(CreateInboundEnvelope(true), null, true).WaitAsync(Timeout);

        result.IsSuccess.ShouldBeTrue();
        result.PushedStreamsCount.ShouldBe(0);
        sequence.Length.ShouldBe(0);
        sequence.GetCommitIdentifiers().ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_ShouldLinkSequence_WhenMessageIsFiltered()
    {
        using TrackingTestSequence sequence = new(CreateContext(new HookLogger()), 2);
        TestSequence childSequence = CreateSequence(new HookLogger()); // Disposed by the parent sequence

        AddToSequenceResult result = await sequence.AddAsync(CreateInboundEnvelope(true), childSequence, false).WaitAsync(Timeout);

        // The filtered message (e.g. chunked) joins the sequence like a message that no stream accepts, so that its sequence is
        // committed or rolled back with it
        result.IsSuccess.ShouldBeTrue();
        childSequence.ParentSequence.ShouldBeSameAs(sequence);
        sequence.Sequences.ShouldBe([childSequence]);
        sequence.Length.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Invoked by the add, before the disposal")]
    public async Task AddAsync_ShouldUnlinkSequence_WhenMessageIsFilteredWhileSequenceIsBeingCompleted(bool isFound)
    {
        using AbortTestSequence sequence = new(CreateErrorPolicyContext(new HookLogger(), new SkipMessageErrorPolicy()));

        // Found in the store by the reader, otherwise it's the new sequence started by the added message
        if (isFound)
            ((ISequenceImplementation)sequence).SetIsNew(false);

        // The sequence starts being completed (e.g. by the max wait time) while the chunk sequence of the filtered message is being
        // linked, after the checks at the beginning of the add: the completion may notify the processing completion without the link
        using HookChildSequence childSequence = new(
            ConsumerPipelineContextHelper.CreateSubstitute(),
            () => typeof(SequenceBase<IRawInboundEnvelope>)
                .GetField("_isCompleting", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(sequence, true));

        AddToSequenceResult result = await sequence.AddAsync(CreateInboundEnvelope(true), childSequence, false).WaitAsync(Timeout);

        // The message is refused like a pushed one (added to the next sequence, or sharing the outcome of the sequence it started),
        // without leaving the chunk sequence linked to this one, which wouldn't complete it
        result.IsSuccess.ShouldBe(!isFound);
        result.IsAborted.ShouldBe(!isFound);
        childSequence.Parent.ShouldBeNull();
        sequence.Sequences.ShouldNotContain(childSequence);
        sequence.Length.ShouldBe(0);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnAborted_WhenFilteredMessageIsAddedToAbortedSequence()
    {
        using TrackingTestSequence sequence = new(CreateContext(new HookLogger()), 2);
        await sequence.AbortAsync(SequenceAbortReason.ConsumerAborted).WaitAsync(Timeout);

        AddToSequenceResult result = await sequence.AddAsync(CreateInboundEnvelope(true), null, false).WaitAsync(Timeout);

        result.IsAborted.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAsync_ShouldElapseTimeoutWhenDue()
    {
        using TimeoutTrackingTestSequence sequence = new(CreateContext(new HookLogger()), TimeSpan.FromSeconds(1), true);

        await sequence.AddAsync(CreateEnvelope(), null, false).WaitAsync(Timeout);
        await Task.Delay(100);
        Stopwatch stopwatch = Stopwatch.StartNew();
        await sequence.AddAsync(CreateEnvelope(), null, false).WaitAsync(Timeout);
        await sequence.TimeoutElapsed.WaitAsync(Timeout);

        // The timeout elapses 1 second after the last message, instead of at the next timer tick (every second since the first message,
        // about 1.9 seconds after the last one)
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(990));
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromMilliseconds(1600));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAsync_ShouldMeasureTimeoutAccordingToTimeoutMode(bool isAbsolute)
    {
        SequenceTimeoutMode timeoutMode = isAbsolute ? SequenceTimeoutMode.Absolute : SequenceTimeoutMode.Sliding;
        TimeSpan timeout = TimeSpan.FromSeconds(1);
        using TimeoutTrackingTestSequence sequence = new(CreateContext(new HookLogger()), timeout, true, timeoutMode);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan lastAddTime = TimeSpan.Zero;
        TimeSpan maxGapBetweenAdds = TimeSpan.Zero;

        // The messages keep arriving more often than the timeout (1 second), for more than twice the timeout
        while (stopwatch.Elapsed < TimeSpan.FromMilliseconds(2500) && !sequence.TimeoutElapsed.IsCompleted)
        {
            await sequence.AddAsync(CreateEnvelope(), null, false).WaitAsync(Timeout);

            TimeSpan addTime = stopwatch.Elapsed;
            maxGapBetweenAdds = TimeSpan.FromTicks(Math.Max(maxGapBetweenAdds.Ticks, (addTime - lastAddTime).Ticks));
            lastAddTime = addTime;

            await Task.Delay(100);
        }

        bool isTimeoutElapsed = sequence.TimeoutElapsed.IsCompleted;
        maxGapBetweenAdds = TimeSpan.FromTicks(Math.Max(maxGapBetweenAdds.Ticks, (stopwatch.Elapsed - lastAddTime).Ticks));

        if (isAbsolute)
        {
            isTimeoutElapsed.ShouldBeTrue();
            stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(990));
        }
        else
        {
            // The sliding timeout may rightly elapse only if the test runner stalled for longer than the timeout between two messages
            if (maxGapBetweenAdds < timeout * 0.9)
                isTimeoutElapsed.ShouldBeFalse();

            await sequence.TimeoutElapsed.WaitAsync(Timeout);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldEnforceTimeoutAgainAtTimeoutInterval_WhenSequenceIsStillPendingOnceTimeoutElapsed()
    {
        using TimeoutTrackingTestSequence sequence = new(CreateContext(new HookLogger()), TimeSpan.FromMilliseconds(200), false);

        await sequence.AddAsync(CreateEnvelope(), null, false).WaitAsync(Timeout);
        await sequence.TimeoutElapsed.WaitAsync(Timeout);
        await Task.Delay(1000);

        // About 5 times in a second, not in a tight loop
        sequence.TimeoutElapsedCount.ShouldBeInRange(2, 10);
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

    // The child sequence joins the parent while still pending (e.g. the chunk sequence of a binary message added to a batch with its first
    // chunk), then a message is being pushed to it: returns the pending add
    private static async Task<Task<AddToSequenceResult>> AddPendingChildSequenceAsync(
        SequenceBase<IRawInboundEnvelope> parentSequence,
        SequenceBase<IRawInboundEnvelope> childSequence)
    {
        AddToSequenceResult parentResult = await parentSequence.AddAsync(CreateEnvelope(), childSequence, false).WaitAsync(Timeout);
        parentResult.IsSuccess.ShouldBeTrue();

        TaskCompletionSource childMessagePushed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        childSequence.CreateStream<IRawInboundEnvelope>(
        [
            new DelegateMessageFilter(_ =>
            {
                childMessagePushed.TrySetResult();
                return true;
            })
        ]);
        Task<AddToSequenceResult> childAddTask = childSequence.AddAsync(CreateEnvelope(), null, true);
        await childMessagePushed.Task.WaitAsync(Timeout);

        return childAddTask;
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

    private static TestSequence CreateSequence(HookLogger logger, ISequenceStore? sequenceStore = null) =>
        new(CreateContext(logger, sequenceStore));

    private static ConsumerPipelineContext CreateContext(HookLogger logger, ISequenceStore? sequenceStore = null)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker();

            services.AddSingleton<ISilverbackLogger<SequenceBase<IRawInboundEnvelope>>>(logger);
        });

        return ConsumerPipelineContextHelper.CreateSubstitute(
            serviceProvider: serviceProvider,
            sequenceStore: sequenceStore);
    }

    private static RawInboundEnvelope CreateEnvelope() =>
        new(
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

    private static InboundEnvelope<TestEventOne> CreateInboundEnvelope(bool isFiltered = false) =>
        new(
            new TestEventOne(),
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset())
        {
            IsFiltered = isFiltered
        };

    private sealed class TestSequence : SequenceBase<IRawInboundEnvelope>
    {
        public TestSequence(ConsumerPipelineContext context)
            : base("test-sequence", context, enforceTimeout: false, trackIdentifiers: false)
        {
            TotalLength = 1;
        }

        public Task CompleteFromTestAsync() => CompleteAsync();
    }

    // Tracks the identifiers of the added messages and completes once the specified number of messages is added, like a batch
    private sealed class TrackingTestSequence : SequenceBase<IRawInboundEnvelope>
    {
        public TrackingTestSequence(ConsumerPipelineContext context, int totalLength)
            : base("tracking-test-sequence", context, enforceTimeout: false, trackIdentifiers: true)
        {
            TotalLength = totalLength;
        }
    }

    // Adds the message refused because of the abort to the next sequence, like the batches
    private sealed class AbortTestSequence : SequenceBase<IRawInboundEnvelope>
    {
        public AbortTestSequence(ConsumerPipelineContext context, bool trackIdentifiers = false)
            : base("abort-test-sequence", context, enforceTimeout: false, trackIdentifiers: trackIdentifiers)
        {
        }

        private protected override bool IsMessageRefusedByAbortAddedToNextSequence => true;

        public Task CompleteFromTestAsync() => CompleteAsync();
    }

    private sealed class DelegateMessageFilter : IMessageFilter
    {
        private readonly Func<object, bool> _mustProcess;

        public DelegateMessageFilter(Func<object, bool> mustProcess)
        {
            _mustProcess = mustProcess;
        }

        public bool MustProcess(object message) => _mustProcess(message);
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

    // Records the elapsed timeout, then aborts the sequence (as the base class does) or leaves it pending
    private sealed class TimeoutTrackingTestSequence : SequenceBase<IRawInboundEnvelope>
    {
        private readonly TaskCompletionSource _timeoutElapsed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly bool _abortsOnTimeout;

        private int _timeoutElapsedCount;

        public TimeoutTrackingTestSequence(
            ConsumerPipelineContext context,
            TimeSpan timeout,
            bool abortsOnTimeout,
            SequenceTimeoutMode timeoutMode = SequenceTimeoutMode.Sliding)
            : base("timeout-tracking-test-sequence", context, enforceTimeout: true, timeout: timeout, trackIdentifiers: false)
        {
            _abortsOnTimeout = abortsOnTimeout;
            TimeoutMode = timeoutMode;
        }

        public Task TimeoutElapsed => _timeoutElapsed.Task;

        public int TimeoutElapsedCount => Volatile.Read(ref _timeoutElapsedCount);

        private protected override SequenceTimeoutMode TimeoutMode { get; }

        protected override Task OnTimeoutElapsedAsync()
        {
            Interlocked.Increment(ref _timeoutElapsedCount);
            _timeoutElapsed.TrySetResult();
            return _abortsOnTimeout ? base.OnTimeoutElapsedAsync() : Task.CompletedTask;
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
