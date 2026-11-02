// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
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

    private async Task CompleteAndWaitForSubscriberAsync(Task subscribeTask)
    {
        await _streamProvider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await subscribeTask.WaitAsync(Timeout);
    }
}
