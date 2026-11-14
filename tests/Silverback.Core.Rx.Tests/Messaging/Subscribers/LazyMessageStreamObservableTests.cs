// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging.Subscribers;

public class LazyMessageStreamObservableTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldDetachStream_WhenOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<object> provider = new(detachesReturningSubscribers: true);
        LazyMessageStreamObservable<int> lazyObservable = new(provider.CreateLazyStream<int>());
        provider.CreateLazyStream<string>();
        Task<int> otherPushTask = provider.PushAsync("other");
        Task<int> pushTask = provider.PushAsync(1);

        lazyObservable.NotifySubscriberCompleted(false, false);

        // The pending push and the next ones are acknowledged without being received
        (await pushTask.WaitAsync(Timeout)).ShouldBe(1);
        (await provider.PushAsync(2).WaitAsync(Timeout)).ShouldBe(1);

        provider.Abort();
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldEndStreams_WhenCanceledWhileOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<object> provider = new(detachesReturningSubscribers: true);
        LazyMessageStreamObservable<int> lazyObservable = new(provider.CreateLazyStream<int>());
        provider.CreateLazyStream<string>();
        Task<int> otherPushTask = provider.PushAsync("other");
        Task<int> pushTask = provider.PushAsync(1);

        lazyObservable.NotifySubscriberCompleted(false, true);

        // The pending push is left to the abort, which ends the streams of all the subscribers
        await provider.SubscribersReturnedTask.WaitAsync(Timeout);
        pushTask.IsCompleted.ShouldBeFalse();
        provider.Abort();
        await ShouldBeCanceledAsync(pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    private static async Task ShouldBeCanceledAsync(Task task)
    {
        Exception? exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout));
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }
}
