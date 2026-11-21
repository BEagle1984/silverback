// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Types;
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

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldRethrowObserverExceptionAndReportThrown_WhenSubscriberReturned()
    {
        MessageStreamProvider<object> provider = new(detachesReturningSubscribers: true);
        ILazyMessageStreamEnumerable<int> lazyStream = provider.CreateLazyStream<int>();
        LazyMessageStreamObservable<int> lazyObservable = new(lazyStream);
        provider.CreateLazyStream<string>();
        Task<int> otherPushTask = provider.PushAsync("other");
        Task<int> pushTask = provider.PushAsync(1);

        await SubscribeBehindOperatorAsync(lazyObservable, new TestException());

        Exception? exception = Record.Exception(() => lazyObservable.NotifySubscriberCompleted(false, false));

        exception.ShouldBeOfType<TestException>();

        // Reported as thrown, the stream isn't detached and the pending push is left to the abort
        ((ILazyMessageStreamEnumerable)lazyStream).HasSubscriberReturned.ShouldBeFalse();
        pushTask.IsCompleted.ShouldBeFalse();
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();

        provider.Abort();
        await ShouldBeCanceledAsync(pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldNotRethrowObserverException_WhenSubscriberThrew()
    {
        MessageStreamProvider<object> provider = new(detachesReturningSubscribers: true);
        ILazyMessageStreamEnumerable<int> lazyStream = provider.CreateLazyStream<int>();
        LazyMessageStreamObservable<int> lazyObservable = new(lazyStream);
        Task<int> pushTask = provider.PushAsync(1);

        await SubscribeBehindOperatorAsync(lazyObservable, new TestException());

        Exception? exception = Record.Exception(() => lazyObservable.NotifySubscriberCompleted(true, false));

        exception.ShouldBeNull();
        ((ILazyMessageStreamEnumerable)lazyStream).HasSubscriberReturned.ShouldBeFalse();

        provider.Abort();
        await ShouldBeCanceledAsync(pushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldReportCanceled_WhenCanceledAndObserverThrewCancellation()
    {
        MessageStreamProvider<object> provider = new(detachesReturningSubscribers: true);
        LazyMessageStreamObservable<int> lazyObservable = new(provider.CreateLazyStream<int>());
        provider.CreateLazyStream<string>();
        Task<int> otherPushTask = provider.PushAsync("other");
        Task<int> pushTask = provider.PushAsync(1);

        await SubscribeBehindOperatorAsync(lazyObservable, new OperationCanceledException());

        Exception? exception = Record.Exception(() => lazyObservable.NotifySubscriberCompleted(false, true));

        exception.ShouldBeOfType<OperationCanceledException>();

        // Reported as canceled, the streams of all the subscribers are ended right away
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeTrue();

        provider.Abort();
        await ShouldBeCanceledAsync(pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldReportFailed_WhenCanceledButObserverThrewOtherException()
    {
        MessageStreamProvider<object> provider = new(detachesReturningSubscribers: true);
        LazyMessageStreamObservable<int> lazyObservable = new(provider.CreateLazyStream<int>());
        provider.CreateLazyStream<string>();
        Task<int> otherPushTask = provider.PushAsync("other");
        Task<int> pushTask = provider.PushAsync(1);

        await SubscribeBehindOperatorAsync(lazyObservable, new TestException());

        Exception? exception = Record.Exception(() => lazyObservable.NotifySubscriberCompleted(false, true));

        exception.ShouldBeOfType<TestException>();

        // Reported as failed, the streams are left to the abort, which applies the error policies
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();

        provider.Abort();
        await ShouldBeCanceledAsync(pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    // The operator swallows the exception thrown by the observer, so Subscribe returns normally
    private static async Task SubscribeBehindOperatorAsync(LazyMessageStreamObservable<int> lazyObservable, Exception observerException)
    {
        await lazyObservable.WaitUntilCreatedAsync().WaitAsync(Timeout);
        IObservable<int> observable = lazyObservable.Value.ShouldBeAssignableTo<IObservable<int>>().ShouldNotBeNull();

        await Task.Run(() => observable.Select(message => message).Subscribe(_ => throw observerException)).WaitAsync(Timeout);
    }

    private static async Task ShouldBeCanceledAsync(Task task)
    {
        Exception? exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout));
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }
}
