// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages;

public class MessageStreamProviderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private interface IEvent : IMessage;

    [Fact]
    public async Task PushAsync_ShouldRelayToMatchingStreams()
    {
        MessageStreamProvider<IMessage> provider = new();

        List<IEvent> eventsList = [];
        List<TestEventOne> testEventOnesList = [];

        IMessageStreamEnumerable<IEvent> eventsStream = provider.CreateStream<IEvent>();
        IMessageStreamEnumerable<TestEventOne> testEventOneStream = provider.CreateStream<TestEventOne>();

        Task<List<IEvent>> task1 = Task.Run(() => eventsList = [.. eventsStream]);
        Task<List<TestEventOne>> task2 = Task.Run(() => testEventOnesList = [.. testEventOneStream.ToList()]);

        await provider.PushAsync(new TestEventOne());
        await provider.PushAsync(new TestEventTwo());
        await provider.PushAsync(new TestCommandOne(), false);

        await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

        await Task.WhenAll(task1, task2);

        eventsList.Count.ShouldBe(2);
        eventsList[0].ShouldBeOfType<TestEventOne>();
        eventsList[1].ShouldBeOfType<TestEventTwo>();

        testEventOnesList.Count.ShouldBe(1);
        testEventOnesList[0].ShouldBeOfType<TestEventOne>();
    }

    [Fact]
    public async Task PushAsync_ShouldReturnAfterMessagesProcessed()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<IEvent> stream = provider.CreateStream<IEvent>();

        bool processed = false;

        Task enumerationTask = Task.Run(async () =>
        {
            await Task.Delay(50);
            using IEnumerator<IEvent> enumerator = stream.GetEnumerator();
            enumerator.MoveNext();
            await Task.Delay(50);
            processed = true;
            using IEnumerator<IEvent> enumerator2 = stream.GetEnumerator();
            enumerator2.MoveNext();
        });

        await provider.PushAsync(new TestEventOne());

        processed.ShouldBeTrue();

        await provider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await enumerationTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task PushAsync_ShouldThrow_WhenNoMatchingStream()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<TestEventOne> stream = provider.CreateStream<TestEventOne>();

        Task enumerationTask = Task.Run(() => stream.ToList());

        await provider.PushAsync(new TestEventOne());
        Func<Task> act = () => provider.PushAsync(new TestEventTwo());

        await act.ShouldThrowAsync<UnhandledMessageException>();

        await provider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await enumerationTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task PushAsync_ShouldNotThrow_WhenNoMatchingStreamButExceptionDisabled()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<TestEventOne> stream = provider.CreateStream<TestEventOne>();

        Task enumerationTask = Task.Run(() => stream.ToList());

        await provider.PushAsync(new TestEventOne());
        Func<Task> act = () => provider.PushAsync(new TestEventTwo(), false);

        await act.ShouldNotThrowAsync();

        await provider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await enumerationTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task PushAsync_ShouldRelayEnvelope()
    {
        MessageStreamProvider<TestEnvelope> provider = new();
        IMessageStreamEnumerable<TestEnvelope> stream = provider.CreateStream<TestEnvelope>();

        List<TestEnvelope>? envelopes = null;

        Task<List<TestEnvelope>> task = Task.Run(() => envelopes = [.. stream]);

        await provider.PushAsync(new TestEnvelope(new TestEventOne()));
        await provider.PushAsync(new TestEnvelope(new TestEventTwo()));
        await provider.PushAsync(new TestEnvelope(new TestCommandOne()));

        await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

        await task;

        envelopes.ShouldNotBeNull();
        envelopes.Count.ShouldBe(3);
    }

    [Fact]
    public async Task PushAsync_ShouldUnwrapEnvelope()
    {
        MessageStreamProvider<TestEnvelope> provider = new();
        IMessageStreamEnumerable<IEvent> eventsStream = provider.CreateStream<IEvent>();
        IMessageStreamEnumerable<TestEventOne> testEventOnesStream = provider.CreateStream<TestEventOne>();

        List<IEvent> eventsList = [];
        List<TestEventOne> testEventOnesList = [];

        Task<List<IEvent>> task1 = Task.Run(() => eventsList = [.. eventsStream]);
        Task<List<TestEventOne>> task2 = Task.Run(() => testEventOnesList = [.. testEventOnesStream]);

        await provider.PushAsync(new TestEnvelope(new TestEventOne()), false);
        await provider.PushAsync(new TestEnvelope(new TestEventTwo()), false);
        await provider.PushAsync(new TestEnvelope(new TestCommandOne()), false);

        await provider.CompleteAsync(); // Implicitly tests that the Complete call is also propagated

        await Task.WhenAll(task1, task2);

        eventsList.Count.ShouldBe(2);
        eventsList[0].ShouldBeOfType<TestEventOne>();
        eventsList[1].ShouldBeOfType<TestEventTwo>();

        testEventOnesList.Count.ShouldBe(1);
        testEventOnesList[0].ShouldBeOfType<TestEventOne>();
    }

    [Fact]
    public async Task PushAsync_ShouldThrow_WhenEnvelopeNoMatchingStream()
    {
        MessageStreamProvider<IEnvelope> provider = new();
        IMessageStreamEnumerable<TestEventOne> stream = provider.CreateStream<TestEventOne>();

        Task enumerationTask = Task.Run(() => stream.ToList());

        await provider.PushAsync(new TestEnvelope(new TestEventOne()));
        Func<Task> act = () => provider.PushAsync(new TestEnvelope(new TestEventTwo()));

        await act.ShouldThrowAsync<UnhandledMessageException>();

        await provider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await enumerationTask.WaitAsync(Timeout);
    }

    [Fact]
    public async Task PushAsync_ShouldHandleBackpressureWhileEnumeratingStream()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();
        using IEnumerator<int> enumerator = stream.GetEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        Task<int> pushTask3 = provider.PushAsync(3);

        enumerator.MoveNext();

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        enumerator.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.ShouldBeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();
        pushTask3.IsCompleted.ShouldBeFalse();

        enumerator.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.ShouldBeTrue();
        pushTask3.IsCompleted.ShouldBeFalse();

        await AbortAndWaitForPendingPushAsync(provider, pushTask3);
    }

    [Fact]
    public async Task PushAsync_ShouldHandleBackpressureWhileAsyncEnumeratingStream()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();
        await using IAsyncEnumerator<int> enumerator = stream.GetAsyncEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        Task<int> pushTask3 = provider.PushAsync(3);

        await enumerator.MoveNextAsync();

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        await enumerator.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.ShouldBeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();
        pushTask3.IsCompleted.ShouldBeFalse();

        await enumerator.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.ShouldBeTrue();
        pushTask3.IsCompleted.ShouldBeFalse();

        await AbortAndWaitForPendingPushAsync(provider, pushTask3);
    }

    [Fact]
    public async Task PushAsync_ShouldHandleBackpressureWhileEnumeratingMultipleStreams()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream1 = provider.CreateStream<int>();
        IMessageStreamEnumerable<int> stream2 = provider.CreateStream<int>();
        using IEnumerator<int> enumerator1 = stream1.GetEnumerator();
        using IEnumerator<int> enumerator2 = stream2.GetEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        Task<int> pushTask3 = provider.PushAsync(3);

        enumerator1.MoveNext();
        enumerator2.MoveNext();

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        enumerator1.MoveNext();

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        enumerator2.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.ShouldBeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();

        enumerator1.MoveNext();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();

        enumerator2.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.ShouldBeTrue();

        await AbortAndWaitForPendingPushAsync(provider, pushTask3);
    }

    [Fact]
    public async Task PushAsync_ShouldHandleBackpressureWhileAsyncEnumeratingMultipleStreams_BackpressureIsHandled()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream1 = provider.CreateStream<int>();
        IMessageStreamEnumerable<int> stream2 = provider.CreateStream<int>();
        await using IAsyncEnumerator<int> enumerator1 = stream1.GetAsyncEnumerator();
        await using IAsyncEnumerator<int> enumerator2 = stream2.GetAsyncEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        Task<int> pushTask3 = provider.PushAsync(3);

        await enumerator1.MoveNextAsync();
        await enumerator2.MoveNextAsync();

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        await enumerator1.MoveNextAsync();

        await Task.Delay(100);
        pushTask1.IsCompleted.ShouldBeFalse();

        await enumerator2.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.ShouldBeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();

        await enumerator1.MoveNextAsync();

        await Task.Delay(100);
        pushTask2.IsCompleted.ShouldBeFalse();

        await enumerator2.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.ShouldBeTrue();

        await AbortAndWaitForPendingPushAsync(provider, pushTask3);
    }

    [Fact]
    public async Task CompleteAsync_ShouldCompleteEnumeration()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(() =>
        {
            count = stream.Count();
        });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.ShouldBeNull();

        await provider.CompleteAsync();
        await enumerationTask;

        count.ShouldBe(3);
    }

    [Fact]
    public async Task CompleteAsync_ShouldCompleteAsyncEnumeration()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(async () =>
        {
            count = await stream.CountAsync();
        });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.ShouldBeNull();

        await provider.CompleteAsync();
        await enumerationTask;

        count.ShouldBe(3);
    }

    [Fact]
    public async Task Abort_ShouldAbortEnumeration()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(() =>
        {
            count = stream.Count();
        });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.ShouldBeNull();

        provider.Abort();

        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        count.ShouldBeNull();
        enumerationTask.Status.ShouldBe(TaskStatus.Faulted);
    }

    [Fact]
    public async Task Abort_ShouldCancelPendingAdd()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(() =>
        {
            count = stream.Count();
        });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.ShouldBeNull();

        provider.Abort();

        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        count.ShouldBeNull();
        enumerationTask.Status.ShouldBe(TaskStatus.Faulted);
    }

    [Fact]
    public async Task Abort_ShouldAbortAsyncEnumeration()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(async () =>
        {
            count = await stream.CountAsync();
        });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.ShouldBeNull();

        provider.Abort();

        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        count.ShouldBeNull();
        enumerationTask.Status.ShouldBe(TaskStatus.Canceled);
    }

    [Fact]
    public async Task CreateLazyStream_ShouldCreateStream_WhenMatchingMessagePushed()
    {
        MessageStreamProvider<IEvent> provider = new();
        ILazyMessageStreamEnumerable<TestEventTwo> lazyStream = provider.CreateLazyStream<TestEventTwo>();
        List<TestEventTwo> receivedTwos = [];
        bool completed = false;

        Task.Run(async () =>
        {
            await lazyStream.WaitUntilCreatedAsync();
            await foreach (TestEventTwo message in lazyStream.Stream!)
            {
                receivedTwos.Add(message);
            }

            completed = true;
        }).FireAndForget();

        Task createStreamTask = lazyStream.WaitUntilCreatedAsync();

        await provider.PushAsync(new TestEventOne(), false);
        await provider.PushAsync(new TestEventOne(), false);

        createStreamTask.Status.ShouldNotBe(TaskStatus.RanToCompletion);
        lazyStream.Stream.ShouldBeNull();

        await provider.PushAsync(new TestEventTwo());

        createStreamTask.Status.ShouldBe(TaskStatus.RanToCompletion);
        lazyStream.Stream.ShouldBeOfType<MessageStreamEnumerable<TestEventTwo>>();

        await provider.PushAsync(new TestEventTwo());

        receivedTwos.Count.ShouldBe(2);

        await provider.CompleteAsync();

        await AsyncTestingUtil.WaitAsync(() => completed);
        completed.ShouldBeTrue();
    }

    [Fact]
    public void CreateLazyStream_ShouldReturnNewInstance_WhenCalledTwiceForSameType()
    {
        MessageStreamProvider<IMessage> provider = new();
        ILazyMessageStreamEnumerable<IEvent> stream1 = provider.CreateLazyStream<IEvent>();
        ILazyMessageStreamEnumerable<IEvent> stream2 = provider.CreateLazyStream<IEvent>();

        stream2.ShouldNotBeSameAs(stream1);
    }

    [Fact]
    public void CreateLazyStream_ShouldReturnEquivalentInstance_WhenGenericAndNonGenericVersionsAreUsed()
    {
        MessageStreamProvider<IMessage> provider = new();

        ILazyMessageStreamEnumerable<IEvent> stream1 = provider.CreateLazyStream<IEvent>();
        ILazyMessageStreamEnumerable<object> stream2 = provider.CreateLazyStream<IEvent>();

        stream2.ShouldBeOfType(stream1.GetType());
    }

    [Fact]
    public async Task PushAsync_ShouldBeCanceled_WhenProviderAbortedBeforeLazyStreamCreation()
    {
        using ManualResetEventSlim filterEntered = new();
        using ManualResetEventSlim continuePush = new();
        MessageStreamProvider<IMessage> provider = new();
        provider.CreateLazyStream<TestEventOne>([new BlockingMessageFilter(filterEntered, continuePush)]);

        Task<int> pushTask = Task.Run(() => provider.PushAsync(new TestEventOne()));

        try
        {
            filterEntered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            provider.Abort();
        }
        finally
        {
            continuePush.Set();
        }

        Func<Task> act = async () => await pushTask;
        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PushAsync_ShouldBeCanceled_WhenProviderCompletedBeforeLazyStreamCreation()
    {
        using ManualResetEventSlim filterEntered = new();
        using ManualResetEventSlim continuePush = new();
        MessageStreamProvider<IMessage> provider = new();
        provider.CreateLazyStream<TestEventOne>([new BlockingMessageFilter(filterEntered, continuePush)]);

        Task<int> pushTask = Task.Run(() => provider.PushAsync(new TestEventOne()));

        try
        {
            filterEntered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            await provider.CompleteAsync();
        }
        finally
        {
            continuePush.Set();
        }

        Func<Task> act = async () => await pushTask;
        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SubscribersReturnedTask_ShouldComplete_WhenSubscribersOfAllCreatedStreamsReturned()
    {
        MessageStreamProvider<IMessage> provider = new();
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream1 = provider.CreateLazyStream<TestEventOne>();
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream2 = provider.CreateLazyStream<TestEventOne>();
        provider.CreateLazyStream<TestEventTwo>();

        Task pushTask = provider.PushAsync(new TestEventOne());

        ((ILazyStreamArgumentValue)lazyStream1).NotifySubscriberCompleted(false, false);
        bool isCompletedBeforeAllReturned = provider.SubscribersReturnedTask.IsCompleted;
        ((ILazyStreamArgumentValue)lazyStream2).NotifySubscriberCompleted(false, false);

        isCompletedBeforeAllReturned.ShouldBeFalse();
        await provider.SubscribersReturnedTask.WaitAsync(Timeout);
        await AbortAndWaitForPendingPushAsync(provider, pushTask);
    }

    [Fact]
    public async Task SubscribersReturnedTask_ShouldNotComplete_WhenSubscriberThrew()
    {
        MessageStreamProvider<IMessage> provider = new();
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();

        Task pushTask = provider.PushAsync(new TestEventOne());

        ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(true, false);
        bool isCompleted = provider.SubscribersReturnedTask.IsCompleted;
        await AbortAndWaitForPendingPushAsync(provider, pushTask);

        isCompleted.ShouldBeFalse();
    }

    [Fact]
    public void SubscribersReturnedTask_ShouldNotComplete_WhenNoStreamCreated()
    {
        MessageStreamProvider<IMessage> provider = new();
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();

        ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);

        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldAcknowledgeReceivedMessageAndReplaceStream_WhenOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        Task otherSubscriberTask = EnumerateAsync(provider.CreateLazyStream<TestEventTwo>());
        await provider.PushAsync(new TestEventTwo()).WaitAsync(Timeout);

        Task<int> pushTask = provider.PushAsync(new TestEventOne());
        await using IAsyncEnumerator<TestEventOne> enumerator = lazyStream.Stream!.GetAsyncEnumerator();
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);

        (await pushTask.WaitAsync(Timeout)).ShouldBe(1);
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();

        ILazyMessageStreamEnumerable<TestEventOne> replacementStream =
            replacement.ShouldBeAssignableTo<ILazyMessageStreamEnumerable<TestEventOne>>().ShouldNotBeNull();
        replacementStream.Stream.ShouldBeNull();

        TestEventOne nextMessage = new();
        Task<int> nextPushTask = provider.PushAsync(nextMessage);
        await replacementStream.WaitUntilCreatedAsync().WaitAsync(Timeout);
        await using IAsyncEnumerator<TestEventOne> replacementEnumerator = replacementStream.Stream!.GetAsyncEnumerator();
        (await replacementEnumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();
        replacementEnumerator.Current.ShouldBeSameAs(nextMessage);

        await AbortAndWaitForPendingPushAsync(provider, nextPushTask);
        await ShouldBeCanceledAsync(otherSubscriberTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldPushMessageNotReceivedToReplacement_WhenOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        Task otherSubscriberTask = EnumerateAsync(provider.CreateLazyStream<TestEventTwo>());
        await provider.PushAsync(new TestEventTwo()).WaitAsync(Timeout);

        Task<int> firstPushTask = provider.PushAsync(new TestEventOne());
        using CancellationTokenSource cancellationTokenSource = new();
        await using IAsyncEnumerator<TestEventOne> enumerator = lazyStream.Stream!.GetAsyncEnumerator(cancellationTokenSource.Token);
        (await enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();

        // The subscriber stops waiting for the next message, then returns once it's pushed
        await cancellationTokenSource.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(enumerator.MoveNextAsync().AsTask().WaitAsync(Timeout));
        await firstPushTask.WaitAsync(Timeout);
        TestEventOne message = new();
        Task<int> pushTask = provider.PushAsync(message);
        await Task.WhenAny(pushTask, Task.Delay(100));
        pushTask.IsCompleted.ShouldBeFalse();

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);

        ILazyMessageStreamEnumerable<TestEventOne> replacementStream =
            replacement.ShouldBeAssignableTo<ILazyMessageStreamEnumerable<TestEventOne>>().ShouldNotBeNull();
        await replacementStream.WaitUntilCreatedAsync().WaitAsync(Timeout);
        await using IAsyncEnumerator<TestEventOne> replacementEnumerator = replacementStream.Stream!.GetAsyncEnumerator();
        (await replacementEnumerator.MoveNextAsync().AsTask().WaitAsync(Timeout)).ShouldBeTrue();
        replacementEnumerator.Current.ShouldBeSameAs(message);

        Task<bool> nextMoveNextTask = replacementEnumerator.MoveNextAsync().AsTask();
        (await pushTask.WaitAsync(Timeout)).ShouldBe(1);

        provider.Abort();
        await ShouldBeCanceledAsync(nextMoveNextTask);
        await ShouldBeCanceledAsync(otherSubscriberTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldCompleteSubscribersReturnedTask_WhenLastEnumeratingSubscriberReturns()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream1 = provider.CreateLazyStream<TestEventOne>();
        ILazyMessageStreamEnumerable<TestEventTwo> lazyStream2 = provider.CreateLazyStream<TestEventTwo>();
        Task<int> pushTask2 = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask1 = provider.PushAsync(new TestEventOne());

        ILazyStreamArgumentValue? replacement1 = ((ILazyStreamArgumentValue)lazyStream1).NotifySubscriberCompleted(false, false);
        ILazyStreamArgumentValue? replacement2 = ((ILazyStreamArgumentValue)lazyStream2).NotifySubscriberCompleted(false, false);

        // The subscriber was invoked for the first message, which is acknowledged without being received
        replacement1.ShouldNotBeNull();
        (await pushTask1.WaitAsync(Timeout)).ShouldBe(1);

        // The replacement stream isn't created yet, so no subscriber is enumerating anymore
        replacement2.ShouldBeNull();
        await provider.SubscribersReturnedTask.WaitAsync(Timeout);
        await AbortAndWaitForPendingPushAsync(provider, pushTask2);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldNotReplaceStream_WhenRestartDisabled()
    {
        MessageStreamProvider<IMessage> provider = new();
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);

        replacement.ShouldBeNull();
        await Task.WhenAny(pushTask, Task.Delay(100));
        pushTask.IsCompleted.ShouldBeFalse();
        await AbortAndWaitForPendingPushAsync(provider, pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldNotReplaceStream_WhenAborted()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());
        await AbortAndWaitForPendingPushAsync(provider, pushTask);

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);

        replacement.ShouldBeNull();
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldCompleteSubscribersReturnedTask_WhenCanceledWhileOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, true);

        // The pending push is left to the abort, which ends the streams of all the subscribers
        replacement.ShouldBeNull();
        await provider.SubscribersReturnedTask.WaitAsync(Timeout);
        pushTask.IsCompleted.ShouldBeFalse();
        await AbortAndWaitForPendingPushAsync(provider, pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldNotReplaceStream_WhenOtherSubscriberThrew()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        ILazyMessageStreamEnumerable<TestEventTwo> otherLazyStream = provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());
        ((ILazyStreamArgumentValue)otherLazyStream).NotifySubscriberCompleted(true, false);

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);

        // The pending push is left to the abort caused by the failure
        replacement.ShouldBeNull();
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();
        pushTask.IsCompleted.ShouldBeFalse();
        await AbortAndWaitForPendingPushAsync(provider, pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldCompleteSubscribersReturnedTask_WhenThrewCancellationWhileOtherSubscriberIsEnumerating()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());

        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(true, true);

        // The pending push is left to the abort, which ends the streams of all the subscribers
        replacement.ShouldBeNull();
        await provider.SubscribersReturnedTask.WaitAsync(Timeout);
        pushTask.IsCompleted.ShouldBeFalse();
        await AbortAndWaitForPendingPushAsync(provider, pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public Task NotifySubscriberCompleted_ShouldNotCompleteSubscribersReturnedTask_WhenSingleSubscriberThrewCancellation()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        Task<int> pushTask = provider.PushAsync(new TestEventOne());

        ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(true, true);

        // The pending push is left to the abort caused by the cancellation, as for a failure
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();
        return AbortAndWaitForPendingPushAsync(provider, pushTask);
    }

    [Fact]
    public async Task NotifySubscriberCompleted_ShouldNotCompleteSubscribersReturnedTask_WhenThrewCancellationAfterOtherSubscriberFailed()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        ILazyMessageStreamEnumerable<TestEventTwo> otherLazyStream = provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());
        ((ILazyStreamArgumentValue)otherLazyStream).NotifySubscriberCompleted(true, false);

        ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(true, true);

        // The pending push is left to the abort caused by the failure, which applies the error policies
        provider.SubscribersReturnedTask.IsCompleted.ShouldBeFalse();
        await AbortAndWaitForPendingPushAsync(provider, pushTask);
        await ShouldBeCanceledAsync(otherPushTask);
    }

    [Fact]
    public async Task Abort_ShouldCancelReplacementStreamCreation()
    {
        MessageStreamProvider<IMessage> provider = new(restartsReturningSubscribers: true);
        ILazyMessageStreamEnumerable<TestEventOne> lazyStream = provider.CreateLazyStream<TestEventOne>();
        provider.CreateLazyStream<TestEventTwo>();
        Task<int> otherPushTask = provider.PushAsync(new TestEventTwo());
        Task<int> pushTask = provider.PushAsync(new TestEventOne());
        ILazyStreamArgumentValue? replacement = ((ILazyStreamArgumentValue)lazyStream).NotifySubscriberCompleted(false, false);
        await pushTask.WaitAsync(Timeout);

        await AbortAndWaitForPendingPushAsync(provider, otherPushTask);

        Func<Task> act = () => replacement.ShouldNotBeNull().WaitUntilCreatedAsync().WaitAsync(Timeout);
        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void CreateStream_ShouldReturnNewInstance_WhenCalledTwiceForSameType()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<IEvent> stream1 = provider.CreateStream<IEvent>();
        IMessageStreamEnumerable<IEvent> stream2 = provider.CreateStream<IEvent>();

        stream2.ShouldNotBeSameAs(stream1);
    }

    [Fact]
    public void CreateStream_ShouldReturnEquivalentInstance_WhenGenericAndNonGenericVersionsAreUsed()
    {
        MessageStreamProvider<IMessage> provider = new();

        IMessageStreamEnumerable<IEvent> stream1 = provider.CreateStream<IEvent>();
        IMessageStreamEnumerable<object> stream2 = provider.CreateStream<IEvent>();

        stream2.ShouldBeOfType(stream1.GetType());
    }

    private static async Task AbortAndWaitForPendingPushAsync(MessageStreamProvider provider, Task pendingPushTask)
    {
        provider.Abort();

        Func<Task> act = () => pendingPushTask.WaitAsync(Timeout);
        await act.ShouldThrowAsync<OperationCanceledException>();
    }

    private static async Task ShouldBeCanceledAsync(Task task)
    {
        Exception? exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout));
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    // Enumerates the stream, once created, until it's completed or aborted
    private static Task EnumerateAsync<TMessage>(ILazyMessageStreamEnumerable<TMessage> lazyStream) =>
        Task.Run(async () =>
        {
            await lazyStream.WaitUntilCreatedAsync();

            await foreach (TMessage dummy in lazyStream.Stream!)
            {
                // Just pull the messages
            }
        });

    private class TestEvent : IEvent;

    private class TestEventOne : TestEvent;

    private class TestEventTwo : TestEvent;

    private class TestCommandOne : IMessage;

    private sealed class BlockingMessageFilter(
        ManualResetEventSlim entered,
        ManualResetEventSlim continueProcessing) : IMessageFilter
    {
        public bool MustProcess(object message)
        {
            entered.Set();
            continueProcessing.Wait();
            return true;
        }
    }

    private class TestEnvelope : IEnvelope
    {
        public TestEnvelope(object? message)
        {
            Message = message;
        }

        public object? Message { get; }

        public Type MessageType => Message?.GetType() ?? typeof(object);
    }
}
