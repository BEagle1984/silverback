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
