// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging.Subscribers;

public class LazyMessageStreamObservableTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldReturnObservableOfReplacementStream_WhenOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<object> provider = new(restartsReturningSubscribers: true);
        LazyMessageStreamObservable<int> lazyObservable = new(provider.CreateLazyStream<int>());
        provider.CreateLazyStream<string>();
        Task<int> otherPushTask = provider.PushAsync("other");
        Task<int> pushTask = provider.PushAsync(1);

        ILazyStreamArgumentValue? replacement = lazyObservable.NotifySubscriberCompleted(false, false);

        await pushTask.WaitAsync(Timeout);
        LazyMessageStreamObservable<int> replacementObservable = replacement.ShouldBeOfType<LazyMessageStreamObservable<int>>();

        Task<int> nextPushTask = provider.PushAsync(2);
        await replacementObservable.WaitUntilCreatedAsync().WaitAsync(Timeout);
        using IDisposable observable = replacementObservable.Value.ShouldBeAssignableTo<IDisposable>().ShouldNotBeNull();
        observable.ShouldBeAssignableTo<IMessageStreamObservable<int>>();

        provider.Abort();
        await ShouldBeCanceledAsync(nextPushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    private static async Task ShouldBeCanceledAsync(Task task)
    {
        Exception? exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout));
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }
}
