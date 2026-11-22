// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequenceExtensionsTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task AwaitProcessingAsync_ShouldAwaitProcessingTask()
    {
        using FakeSequence sequence = new();
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        sequence.Context.ProcessingTask = processingTaskCompletionSource.Task;
        ((ISequenceImplementation)sequence).NotifyProcessingCompleted();

        Task awaitTask = sequence.AwaitProcessingAsync(true);

        await Task.Delay(100);
        awaitTask.IsCompleted.ShouldBeFalse();

        processingTaskCompletionSource.SetResult();
        await awaitTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task AwaitProcessingAsync_ShouldAwaitProcessingCompletion()
    {
        using FakeSequence sequence = new();
        sequence.Context.ProcessingTask = Task.CompletedTask;

        Task awaitTask = sequence.AwaitProcessingAsync(true);

        await Task.Delay(100);
        awaitTask.IsCompleted.ShouldBeFalse();

        ((ISequenceImplementation)sequence).NotifyProcessingCompleted();
        await awaitTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task AwaitProcessingAsync_ShouldIgnoreProcessingTaskException()
    {
        using FakeSequence sequence = new();
        sequence.Context.ProcessingTask = Task.FromException(new InvalidOperationException("test"));
        ((ISequenceImplementation)sequence).NotifyProcessingCompleted();

        Task awaitTask = sequence.AwaitProcessingAsync(true);

        await Should.NotThrowAsync(() => awaitTask.WaitAsync(Timeout));
    }

    [Fact]
    public async Task AwaitProcessingAsync_ShouldRethrowProcessingFailure_WhenRethrowExceptionsIsTrue()
    {
        using FakeSequence sequence = new();
        InvalidOperationException exception = new("test");
        ((ISequenceImplementation)sequence).NotifyProcessingFailed(exception);

        Exception thrownException = await Should.ThrowAsync<InvalidOperationException>(
            () => sequence.AwaitProcessingAsync(true).WaitAsync(Timeout));

        thrownException.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task AwaitProcessingAsync_ShouldNotThrow_WhenProcessingFailedAndRethrowExceptionsIsFalse()
    {
        using FakeSequence sequence = new();
        ((ISequenceImplementation)sequence).NotifyProcessingFailed(new InvalidOperationException("test"));

        Task awaitTask = sequence.AwaitProcessingAsync(false);

        await Should.NotThrowAsync(() => awaitTask.WaitAsync(Timeout));
    }
}
