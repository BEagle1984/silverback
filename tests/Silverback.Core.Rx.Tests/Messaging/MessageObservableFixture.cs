// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging;

[Collection("MessageObservable")]
public sealed class MessageObservableFixture : IDisposable
{
    private readonly MessageStreamProvider<int> _streamProvider;

    private readonly IMessageStreamObservable<int> _observable;

    public MessageObservableFixture()
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

        count.Should().Be(3);
        task.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Subscribe_ShouldComplete_WhenStreamCompletes()
    {
        int count = 0;

        Task<IDisposable> subscribeTask = Task.Run(() => _observable.Subscribe(_ => count++));

        await _streamProvider.PushAsync(1);
        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        await _streamProvider.CompleteAsync();

        await subscribeTask;

        count.Should().Be(3);
    }

    [Fact]
    public async Task Subscribe_ShouldUnblockPush()
    {
        int count = 0;

        Task<int> pushTask = _streamProvider.PushAsync(1);

        await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted, TimeSpan.FromMilliseconds(100));
        pushTask.IsCompleted.Should().BeFalse();

        Task.Run(() => _observable.Subscribe(_ => count++)).FireAndForget();

        await AsyncTestingUtil.WaitAsync(() => pushTask.IsCompleted);
        pushTask.IsCompleted.Should().BeTrue();

        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        count.Should().Be(3);
    }

    [Fact]
    public async Task Subscribe_ShouldThrow_WhenCalledMultipleTimes()
    {
        int count = 0;

        Task firstSubscribe = Task.Run(() => _observable.Subscribe(_ => count++));
        Task secondSubscribe = Task.Run(() => _observable.Subscribe(_ => count++));

        await Task.WhenAny(firstSubscribe, secondSubscribe);

        firstSubscribe.IsFaulted.Should().NotBe(secondSubscribe.IsFaulted); // any of the two must have failed, but not both

        await _streamProvider.PushAsync(1);
        await _streamProvider.PushAsync(2);
        await _streamProvider.PushAsync(3);

        count.Should().Be(3);
    }

    [Fact]
    public async Task Subscribe_ShouldInvokeOnNext_WhenStreamIsPushedFromDifferentThreads()
    {
        int count = 0;

        Task.Run(() => _observable.Subscribe(_ => Interlocked.Increment(ref count))).FireAndForget();

        CountdownEvent countdownEvent = new(3);

        await Task.WhenAll(
            Task.Run(
                async () =>
                {
                    countdownEvent.Signal();
                    countdownEvent.WaitOrThrow();
                    await _streamProvider.PushAsync(1);
                }),
            Task.Run(
                async () =>
                {
                    countdownEvent.Signal();
                    countdownEvent.WaitOrThrow();
                    await _streamProvider.PushAsync(2);
                }),
            Task.Run(
                async () =>
                {
                    countdownEvent.Signal();
                    countdownEvent.WaitOrThrow();
                    await _streamProvider.PushAsync(3);
                }));

        count.Should().Be(3);
    }

    public void Dispose()
    {
        _streamProvider.Dispose();
    }
}
