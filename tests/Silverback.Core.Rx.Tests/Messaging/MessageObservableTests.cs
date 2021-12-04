// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging;

[Collection("MessageObservable")]
public sealed class MessageObservableTests : IDisposable
{
    private readonly MessageStreamProvider<int> _streamProvider;

    private readonly IMessageStreamObservable<int> _observable;

    public MessageObservableTests()
    {
        _streamProvider = new MessageStreamProvider<int>();
        _observable = new MessageStreamObservable<int>(_streamProvider.CreateStream<int>());
    }

    [Fact]
    public async Task Subscribe_MessagesPushed_MessagesReceived()
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
    public async Task Subscribe_StreamPushedAndCompleted_SubscribeReturned()
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
    public async Task Subscribe_StreamPushBlockedUntilSubscribed()
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

    public void Dispose()
    {
        _streamProvider.Dispose();
    }
}
