// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages;

public class MessageStreamEnumerableTests
{
    [Fact]
    public async Task PushAsyncGetEnumeratorAndCompleteAsync_SomeMessages_MessagesPushedAndReceived()
    {
        MessageStreamEnumerable<Message> stream = new();
        bool success = false;

        Task enumerationTask = Task.Run(
            () =>
            {
                using IEnumerator<Message> enumerator = stream.GetEnumerator();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Value.Should().Be(1);
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Value.Should().Be(2);
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Value.Should().Be(3);
                enumerator.MoveNext().Should().BeFalse();
                success = true;
            });

        await stream.PushAsync(new Message(1));
        await stream.PushAsync(new Message(2));
        await stream.PushAsync(new Message(3));

        await stream.CompleteAsync();

        await enumerationTask;

        success.Should().BeTrue();
    }

    [Fact]
    public async Task PushAsyncGetAsyncEnumeratorAndCompleteAsync_SomeMessages_MessagesPushedAndReceived()
    {
        MessageStreamEnumerable<Message> stream = new();
        bool success = false;

        Task enumerationTask = Task.Run(
            async () =>
            {
                await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();
                (await enumerator.MoveNextAsync()).Should().BeTrue();
                enumerator.Current.Value.Should().Be(1);
                (await enumerator.MoveNextAsync()).Should().BeTrue();
                enumerator.Current.Value.Should().Be(2);
                (await enumerator.MoveNextAsync()).Should().BeTrue();
                enumerator.Current.Value.Should().Be(3);
                (await enumerator.MoveNextAsync()).Should().BeFalse();
                success = true;
            });

        await stream.PushAsync(new Message(1));
        await stream.PushAsync(new Message(2));
        await stream.PushAsync(new Message(3));

        await stream.CompleteAsync();

        await enumerationTask;

        success.Should().BeTrue();
    }

    [Fact]
    public async Task PushAsync_WhileEnumerating_BackpressureIsHandled()
    {
        MessageStreamEnumerable<Message> stream = new();
        using IEnumerator<Message> enumerator = stream.GetEnumerator();

        Task pushTask1 = stream.PushAsync(new Message(1));
        Task pushTask2 = stream.PushAsync(new Message(2));
        Task pushTask3 = stream.PushAsync(new Message(3));

        enumerator.MoveNext();

        await Task.Delay(100);
        pushTask1.IsCompleted.Should().BeFalse();

        enumerator.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.Should().BeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.Should().BeFalse();
        pushTask3.IsCompleted.Should().BeFalse();

        enumerator.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.Should().BeTrue();
        pushTask3.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task PushAsync_WhileAsyncEnumerating_BackpressureIsHandled()
    {
        MessageStreamEnumerable<Message> stream = new();
        await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();

        Task pushTask1 = stream.PushAsync(new Message(1));
        Task pushTask2 = stream.PushAsync(new Message(2));
        Task pushTask3 = stream.PushAsync(new Message(3));

        await enumerator.MoveNextAsync();

        await Task.Delay(100);
        pushTask1.IsCompleted.Should().BeFalse();

        await enumerator.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.Should().BeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.Should().BeFalse();
        pushTask3.IsCompleted.Should().BeFalse();

        await enumerator.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.Should().BeTrue();
        pushTask3.IsCompleted.Should().BeFalse();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task CompleteAsync_WhileEnumerating_EnumerationCompleted()
    {
        bool completed = false;
        MessageStreamEnumerable<Message> stream = new();
        using IEnumerator<Message> enumerator = stream.GetEnumerator();

        // The next MoveNext reaches the end of the enumerable
        Task.Run(
            () =>
            {
                enumerator.MoveNext();
                completed = true;
            }).FireAndForget();

        completed.Should().BeFalse();

        await stream.CompleteAsync();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => completed);

        completed.Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task CompleteAsync_WhileAsyncEnumerating_EnumerationCompleted()
    {
        bool completed = false;
        MessageStreamEnumerable<Message> stream = new();
        await using IAsyncEnumerator<Message> enumerator = stream.GetAsyncEnumerator();

        // The next MoveNext reaches the end of the enumerable
        Task.Run(
            async () =>
            {
                await enumerator.MoveNextAsync();
                completed = true;
            }).FireAndForget();

        completed.Should().BeFalse();

        await stream.CompleteAsync();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => completed);

        completed.Should().BeTrue();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task Abort_WhileEnumerating_EnumerationAborted()
    {
        bool completed = false;
        MessageStreamEnumerable<int> stream = new();
        using IEnumerator<int> enumerator = stream.GetEnumerator();

        Task enumerationTask = Task.Run(
            () =>
            {
                enumerator.MoveNext();
                completed = true;
            });

        completed.Should().BeFalse();

        stream.Abort();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        completed.Should().BeFalse();
        enumerationTask.Status.Should().Be(TaskStatus.Faulted);
        enumerationTask.Exception!.InnerExceptions[0].Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the async task to complete.")]
    public async Task Abort_WhileAsyncEnumerating_EnumerationAborted()
    {
        bool completed = false;
        MessageStreamEnumerable<int> stream = new();
        await using IAsyncEnumerator<int> enumerator = stream.GetAsyncEnumerator();

        Task enumerationTask = Task.Run(
            async () =>
            {
                await enumerator.MoveNextAsync();
                completed = true;
            });

        completed.Should().BeFalse();

        stream.Abort();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        completed.Should().BeFalse();
        enumerationTask.Status.Should().Be(TaskStatus.Canceled);
    }

    [Fact]
    public async Task Abort_WhilePushing_PushAborted()
    {
        bool pushed = false;
        MessageStreamEnumerable<Message> stream = new();

        Task pushTask = Task.Run(
            async () =>
            {
                await stream.PushAsync(new Message(42));
                pushed = true;
            });

        pushed.Should().BeFalse();

        stream.Abort();

        // Give the other thread a chance to exit the MoveNext
        await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);

        pushed.Should().BeFalse();
        pushTask.Status.Should().Be(TaskStatus.Canceled);
    }

    [Fact]
    public async Task CompleteAsync_TryPushingAfterComplete_ExceptionThrown()
    {
        MessageStreamEnumerable<Message> stream = new();

        await stream.CompleteAsync();

        Func<Task> act = async () => await stream.PushAsync(new Message(42));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Dispose_TryPushingAfterDispose_ExceptionThrown()
    {
        MessageStreamEnumerable<Message> stream = new();
        stream.Dispose();

        Func<Task> act = async () => await stream.PushAsync(new Message(42));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private record Message(int Value);
}
