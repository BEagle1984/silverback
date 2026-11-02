// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages;

public class MessageStreamEnumerableTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PushAsyncGetEnumeratorAndCompleteAsync_ShouldPush()
    {
        MessageStreamEnumerable<Message> stream = new();
        bool success = false;

        Task enumerationTask = Task.Run(() =>
        {
            using IEnumerator<Message> enumerator = stream.GetEnumerator();
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.Value.ShouldBe(1);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.Value.ShouldBe(2);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.Value.ShouldBe(3);
            enumerator.MoveNext().ShouldBeFalse();
            success = true;
        });

        await stream.PushAsync(new Message(1)).WaitAsync(Timeout);
        await stream.PushAsync(new Message(2)).WaitAsync(Timeout);
        await stream.PushAsync(new Message(3)).WaitAsync(Timeout);

        await stream.CompleteAsync().WaitAsync(Timeout);

        await enumerationTask.WaitAsync(Timeout);

        success.ShouldBeTrue();
    }

    [Fact]
    public async Task PushAsyncGetAsyncEnumeratorAndCompleteAsync_ShouldPush()
    {
        MessageStreamEnumerable<Message> stream = new();
        bool success = false;

        Task enumerationTask = Task.Run(async () =>
        {
            await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            enumerator.Current.Value.ShouldBe(1);
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            enumerator.Current.Value.ShouldBe(2);
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            enumerator.Current.Value.ShouldBe(3);
            (await enumerator.MoveNextAsync()).ShouldBeFalse();
            success = true;
        });

        await stream.PushAsync(new Message(1)).WaitAsync(Timeout);
        await stream.PushAsync(new Message(2)).WaitAsync(Timeout);
        await stream.PushAsync(new Message(3)).WaitAsync(Timeout);

        await stream.CompleteAsync().WaitAsync(Timeout);

        await enumerationTask.WaitAsync(Timeout);

        success.ShouldBeTrue();
    }

    [Fact]
    public async Task PushAsync_ShouldHandleBackpressureWhileEnumerating()
    {
        MessageStreamEnumerable<Message> stream = new();
        using IEnumerator<Message> enumerator = stream.GetEnumerator();

        Task pushTask1 = stream.PushAsync(new Message(1));
        Task pushTask2 = stream.PushAsync(new Message(2));
        Task pushTask3 = stream.PushAsync(new Message(3));

        await MoveNextAsync(enumerator);

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        await MoveNextAsync(enumerator);

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.ShouldBeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();
        pushTask3.IsCompleted.ShouldBeFalse();

        await MoveNextAsync(enumerator);

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.ShouldBeTrue();
        pushTask3.IsCompleted.ShouldBeFalse();

        await AbortAndWaitForPendingPushAsync(stream, pushTask3);
    }

    [Fact]
    public async Task PushAsync_ShouldHandleBackpressureWhileAsyncEnumerating()
    {
        MessageStreamEnumerable<Message> stream = new();
        await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();

        Task pushTask1 = stream.PushAsync(new Message(1));
        Task pushTask2 = stream.PushAsync(new Message(2));
        Task pushTask3 = stream.PushAsync(new Message(3));

        await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout);

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout);

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.ShouldBeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();
        pushTask3.IsCompleted.ShouldBeFalse();

        await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout);

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.ShouldBeTrue();
        pushTask3.IsCompleted.ShouldBeFalse();

        await AbortAndWaitForPendingPushAsync(stream, pushTask3);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task CompleteAsync_ShouldCompleteWhileEnumerating()
    {
        bool completed = false;
        MessageStreamEnumerable<Message> stream = new();
        using IEnumerator<Message> enumerator = stream.GetEnumerator();

        // The next MoveNext reaches the end of the enumerable
        Task.Run(() =>
        {
            enumerator.MoveNext();
            completed = true;
        }).FireAndForget();

        completed.ShouldBeFalse();

        await stream.CompleteAsync();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => completed);

        completed.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task CompleteAsync_ShouldCompleteWhileAsyncEnumerating()
    {
        bool completed = false;
        MessageStreamEnumerable<Message> stream = new();
        await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();

        // The next MoveNext reaches the end of the enumerable
        Task.Run(async () =>
        {
            await enumerator.MoveNextAsync();
            completed = true;
        }).FireAndForget();

        completed.ShouldBeFalse();

        await stream.CompleteAsync();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => completed);

        completed.ShouldBeTrue();
    }

    [Fact]
    public async Task CompleteAsync_ShouldWaitForPendingPushWhileEnumerating()
    {
        MessageStreamEnumerable<Message> stream = new();
        using IEnumerator<Message> enumerator = stream.GetEnumerator();

        // PushAsync takes the write semaphore before returning, so the push is guaranteed to be pending when the completion starts
        Task pushTask = stream.PushAsync(new Message(42));
        Task completeTask = stream.CompleteAsync();

        // The completion waits for the pending push, which in turn waits for the message to be processed
        await Task.Delay(100);
        pushTask.IsCompleted.ShouldBeFalse();
        completeTask.IsCompleted.ShouldBeFalse();

        // The pending message is still delivered
        (await MoveNextAsync(enumerator)).ShouldBeTrue();
        enumerator.Current.Value.ShouldBe(42);
        pushTask.IsCompleted.ShouldBeFalse();
        completeTask.IsCompleted.ShouldBeFalse();

        // Moving forward acknowledges the message, then the enumeration ends
        (await MoveNextAsync(enumerator)).ShouldBeFalse();
        await pushTask.WaitAsync(Timeout);
        await completeTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task CompleteAsync_ShouldWaitForPendingPushWhileAsyncEnumerating()
    {
        MessageStreamEnumerable<Message> stream = new();
        await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();

        // PushAsync takes the write semaphore before returning, so the push is guaranteed to be pending when the completion starts
        Task pushTask = stream.PushAsync(new Message(42));
        Task completeTask = stream.CompleteAsync();

        // The completion waits for the pending push, which in turn waits for the message to be processed
        await Task.Delay(100);
        pushTask.IsCompleted.ShouldBeFalse();
        completeTask.IsCompleted.ShouldBeFalse();

        // The pending message is still delivered
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();
        enumerator.Current.Value.ShouldBe(42);
        pushTask.IsCompleted.ShouldBeFalse();
        completeTask.IsCompleted.ShouldBeFalse();

        // Moving forward acknowledges the message, then the enumeration ends
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeFalse();
        await pushTask.WaitAsync(Timeout);
        await completeTask.WaitAsync(Timeout);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task Abort_ShouldAbortWhileEnumerating()
    {
        bool completed = false;
        MessageStreamEnumerable<int> stream = new();
        using IEnumerator<int> enumerator = stream.GetEnumerator();

        Task enumerationTask = Task.Run(() =>
        {
            enumerator.MoveNext();
            completed = true;
        });

        completed.ShouldBeFalse();

        stream.Abort();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        completed.ShouldBeFalse();
        enumerationTask.Status.ShouldBe(TaskStatus.Faulted);
        enumerationTask.Exception!.InnerExceptions[0].ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task Abort_ShouldAbortWhileAsyncEnumerating()
    {
        bool completed = false;
        MessageStreamEnumerable<int> stream = new();
        await using IAsyncEnumerator<int> enumerator = stream.GetAsyncEnumerator();

        Task enumerationTask = Task.Run(async () =>
        {
            await enumerator.MoveNextAsync();
            completed = true;
        });

        completed.ShouldBeFalse();

        stream.Abort();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        completed.ShouldBeFalse();
        enumerationTask.Status.ShouldBe(TaskStatus.Canceled);
    }

    [Fact]
    public async Task Abort_ShouldAbortWhilePushing()
    {
        MessageStreamEnumerable<Message> stream = new();

        // PushAsync takes the write semaphore before returning, so the push is guaranteed to be pending when the stream is aborted
        Task pushTask = stream.PushAsync(new Message(42));
        pushTask.IsCompleted.ShouldBeFalse();

        stream.Abort();

        Exception? pushException = await Record.ExceptionAsync(() => pushTask.WaitAsync(Timeout));

        pushException.ShouldBeAssignableTo<OperationCanceledException>();
        pushTask.Status.ShouldBe(TaskStatus.Canceled);
    }

    [Fact]
    public async Task PushAsync_ShouldNotBlockCompletion_WhenCanceledWhileWaitingForProcessing()
    {
        MessageStreamEnumerable<int> stream = new();
        await using IAsyncEnumerator<int> enumerator = stream.GetAsyncEnumerator();
        using CancellationTokenSource cancellationTokenSource = new();

        Task pushTask = stream.PushAsync(1, cancellationToken: cancellationTokenSource.Token);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();
        await cancellationTokenSource.CancelAsync();

        Exception? pushException = await Record.ExceptionAsync(() => pushTask.WaitAsync(Timeout));
        pushException.ShouldBeAssignableTo<OperationCanceledException>();

        await stream.CompleteAsync().WaitAsync(Timeout);
    }

    [Fact]
    public async Task PushAsync_ShouldAbortStream_WhenCanceledWhileWaitingForProcessing()
    {
        MessageStreamEnumerable<int> stream = new();
        await using IAsyncEnumerator<int> enumerator = stream.GetAsyncEnumerator();
        using CancellationTokenSource cancellationTokenSource = new();

        Task pushTask = stream.PushAsync(1, cancellationToken: cancellationTokenSource.Token);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();
        await cancellationTokenSource.CancelAsync();
        Exception? pushException = await Record.ExceptionAsync(() => pushTask.WaitAsync(Timeout));

        // The late processed signal of the pulled message must not acknowledge the next push
        Exception? nextPullException = await Record.ExceptionAsync(() => enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout));
        Exception? nextPushException = await Record.ExceptionAsync(() => stream.PushAsync(2).WaitAsync(Timeout));

        pushException.ShouldBeAssignableTo<OperationCanceledException>();
        nextPullException.ShouldBeAssignableTo<OperationCanceledException>();
        nextPushException.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task CompleteAsync_ShouldCauseExceptionToBeThrown_WhenTryPushingAfterComplete()
    {
        MessageStreamEnumerable<Message> stream = new();

        await stream.CompleteAsync();

        Func<Task> act = async () => await stream.PushAsync(new Message(42));
        await act.ShouldThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Dispose_ShouldCauseExceptionToBeThrown_WhenTryPushingAfterDispose()
    {
        MessageStreamEnumerable<Message> stream = new();
        stream.Dispose();

        Func<Task> act = async () => await stream.PushAsync(new Message(42));
        await act.ShouldThrowAsync<ObjectDisposedException>();
    }

    // The synchronous enumeration blocks the calling thread, so it is run on the thread pool to be able to time out
    private static Task<bool> MoveNextAsync(IEnumerator<Message> enumerator) =>
        Task.Run(enumerator.MoveNext).WaitAsync(Timeout);

    private static async Task AbortAndWaitForPendingPushAsync(MessageStreamEnumerable<Message> stream, Task pendingPushTask)
    {
        stream.Abort();

        Func<Task> act = () => pendingPushTask.WaitAsync(Timeout);
        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    private record Message(int Value);
}
