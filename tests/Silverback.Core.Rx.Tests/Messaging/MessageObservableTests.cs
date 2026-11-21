// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging;

[Collection("MessageObservable")]
public sealed class MessageObservableTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly MessageStreamProvider<int> _streamProvider;

    private readonly IMessageStreamObservable<int> _observable;

    public MessageObservableTests()
    {
        _streamProvider = new MessageStreamProvider<int>();
        _observable = new MessageStreamObservable<int>(_streamProvider.CreateStream<int>());
    }

    [Fact]
    public async Task Subscribe_ShouldInvokeOnNextDelegate_WhenMessageIsPushed()
    {
        int count = 0;

        Task<IDisposable> task = Task.Run(() => _observable.Subscribe(_ => count++));

        await _streamProvider.PushAsync(1);
        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        count.ShouldBe(3);
        task.IsCompleted.ShouldBeFalse();

        await CompleteAndWaitForSubscriberAsync(task);
    }

    [Fact]
    public async Task Subscribe_ShouldComplete_WhenStreamCompletes()
    {
        int count = 0;

        Task<IDisposable> subscribeTask = Task.Run(() => _observable.Subscribe(_ => count++));

        await _streamProvider.PushAsync(1);
        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        await _streamProvider.CompleteAsync().AsTask().WaitAsync(Timeout);

        await subscribeTask.WaitAsync(Timeout);

        count.ShouldBe(3);
    }

    [Fact]
    public async Task Subscribe_ShouldUnblockPush()
    {
        int count = 0;

        Task<int> pushTask = _streamProvider.PushAsync(1);

        await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted, TimeSpan.FromMilliseconds(100));
        pushTask.IsCompleted.ShouldBeFalse();

        Task subscribeTask = Task.Run(() => _observable.Subscribe(_ => count++));

        await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
        pushTask.IsCompleted.ShouldBeTrue();

        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        count.ShouldBe(3);

        await CompleteAndWaitForSubscriberAsync(subscribeTask);
    }

    [Fact]
    public async Task Subscribe_ShouldThrow_WhenCalledMultipleTimes()
    {
        int count = 0;

        Task firstSubscribe = Task.Run(() => _observable.Subscribe(_ => count++));
        Task secondSubscribe = Task.Run(() => _observable.Subscribe(_ => count++));

        await Task.WhenAny(firstSubscribe, secondSubscribe);

        firstSubscribe.IsFaulted.ShouldNotBe(secondSubscribe.IsFaulted); // any of the two must have failed, but not both

        await _streamProvider.PushAsync(1);
        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        count.ShouldBe(3);

        await CompleteAndWaitForSubscriberAsync(firstSubscribe.IsFaulted ? secondSubscribe : firstSubscribe);
    }

    [Fact]
    public async Task Constructor_ShouldNotEnumerateStream()
    {
        using MessageStreamProvider<int> streamProvider = new();
        EnumerationTrackingStream<int> stream = new(streamProvider.CreateStream<int>());
        IMessageStreamObservable<int> observable = new MessageStreamObservable<int>(stream);
        int count = 0;

        Task<int> pushTask = streamProvider.PushAsync(1);

        await Task.WhenAny(pushTask, Task.Delay(100));
        pushTask.IsCompleted.ShouldBeFalse();
        stream.EnumerationsCount.ShouldBe(0);

        Task subscribeTask = Task.Run(() => observable.Subscribe(_ => Interlocked.Increment(ref count)));

        await pushTask.WaitAsync(Timeout);
        count.ShouldBe(1);
        stream.EnumerationsCount.ShouldBe(1);

        await streamProvider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await subscribeTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task Subscribe_ShouldRethrowExceptionWithOriginalStackTrace_WhenObserverThrows()
    {
        Task subscribeTask = Task.Run(() => _observable.Subscribe(_ => ThrowTestException()));

        _streamProvider.PushAsync(1).FireAndForget();

        Exception? exception = await Record.ExceptionAsync(() => subscribeTask.WaitAsync(Timeout));

        // Abort the pending push before asserting, otherwise disposing the provider would wait for it
        _streamProvider.Abort();

        exception.ShouldBeOfType<TestException>();
        exception.StackTrace.ShouldNotBeNull();
        exception.StackTrace.ShouldContain(nameof(ThrowTestException));
    }

    [Fact]
    public async Task Subscribe_ShouldInvokeOnCompleted_WhenObserverThrows()
    {
        bool isCompleted = false;

        Task subscribeTask = Task.Run(() => _observable.Subscribe(_ => throw new TestException(), () => isCompleted = true));

        _streamProvider.PushAsync(1).FireAndForget();

        Exception? exception = await Record.ExceptionAsync(() => subscribeTask.WaitAsync(Timeout));

        // Abort the pending push before asserting, otherwise disposing the provider would wait for it
        _streamProvider.Abort();

        exception.ShouldBeOfType<TestException>();
        isCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Subscribe_ShouldRethrowOnNextException_WhenObserverThrowsAlsoFromOnCompleted()
    {
        Task subscribeTask = Task.Run(() => _observable.Subscribe(
            _ => throw new TestException(),
            () => throw new InvalidOperationException("Test")));

        _streamProvider.PushAsync(1).FireAndForget();

        Exception? exception = await Record.ExceptionAsync(() => subscribeTask.WaitAsync(Timeout));

        // Abort the pending push before asserting, otherwise disposing the provider would wait for it
        _streamProvider.Abort();

        exception.ShouldBeOfType<TestException>();
    }

    [Fact]
    public async Task Subscribe_ShouldRethrow_WhenObserverThrowsFromOnCompleted()
    {
        Task subscribeTask = Task.Run(() => _observable.Subscribe(_ => { }, () => throw new TestException()));

        await _streamProvider.PushAsync(1);
        await _streamProvider.CompleteAsync().AsTask().WaitAsync(Timeout);

        Exception? exception = await Record.ExceptionAsync(() => subscribeTask.WaitAsync(Timeout));

        exception.ShouldBeOfType<TestException>();
    }

    [Fact]
    public async Task Subscribe_ShouldThrow_WhenCalledAgainAfterCompletion()
    {
        int count = 0;

        Task subscribeTask = Task.Run(() => _observable.Subscribe(_ => count++));

        await _streamProvider.PushAsync(1);
        await CompleteAndWaitForSubscriberAsync(subscribeTask);

        Action act = () => _observable.Subscribe(_ => count++);

        act.ShouldThrow<InvalidOperationException>();
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Subscribe_ShouldInvokeOnNext_WhenStreamIsPushedFromDifferentThreads()
    {
        int count = 0;

        Task subscribeTask = Task.Run(() => _observable.Subscribe(_ => Interlocked.Increment(ref count)));

        CountdownEvent countdownEvent = new(3);

        await Task.WhenAll(
            Task.Run(async () =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();
                await _streamProvider.PushAsync(1);
            }),
            Task.Run(async () =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();
                await _streamProvider.PushAsync(2);
            }),
            Task.Run(async () =>
            {
                countdownEvent.Signal();
                countdownEvent.WaitOrThrow();
                await _streamProvider.PushAsync(3);
            }));

        count.ShouldBe(3);

        await CompleteAndWaitForSubscriberAsync(subscribeTask);
    }

    public void Dispose() => _streamProvider.Dispose();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTestException() => throw new TestException();

    private async Task CompleteAndWaitForSubscriberAsync(Task subscribeTask)
    {
        await _streamProvider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await subscribeTask.WaitAsync(Timeout);
    }

    private sealed class EnumerationTrackingStream<TMessage> : IMessageStreamEnumerable<TMessage>
    {
        private readonly IMessageStreamEnumerable<TMessage> _stream;

        private int _enumerationsCount;

        public EnumerationTrackingStream(IMessageStreamEnumerable<TMessage> stream)
        {
            _stream = stream;
        }

        public int EnumerationsCount => Volatile.Read(ref _enumerationsCount);

        public IEnumerator<TMessage> GetEnumerator()
        {
            Interlocked.Increment(ref _enumerationsCount);
            return _stream.GetEnumerator();
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _enumerationsCount);
            return _stream.GetAsyncEnumerator(cancellationToken);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
