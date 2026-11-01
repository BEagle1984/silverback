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
            "{sequenceType} '{sequenceId}' is completing (total length {sequenceLength})...",
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
        logger.OnLog("Dispose is waiting complete semaphore ({sequenceType} '{sequenceId}')...", disposeHoldsAddSemaphore.Set);

        Task abortTask = Task.Run(() => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted));
        await abortHoldsCompleteSemaphoreTaskCompletionSource.Task.WaitAsync(Timeout);
        Task disposeTask = Task.Run(sequence.Dispose);

        await Task.WhenAll(abortTask, disposeTask).WaitAsync(Timeout);

        sequence.IsAborted.ShouldBeTrue();
        await sequenceStore.Received(1).RemoveAsync(sequence.SequenceId);
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
