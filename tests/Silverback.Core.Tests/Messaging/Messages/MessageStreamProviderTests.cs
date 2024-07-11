// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Messages;

public class MessageStreamProviderTests
{
    private interface IEvent : IMessage;

    [Fact]
    public async Task PushAsync_Messages_RelayedToMatchingStreams()
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

        eventsList.Should().HaveCount(2);
        eventsList[0].Should().BeOfType<TestEventOne>();
        eventsList[1].Should().BeOfType<TestEventTwo>();

        testEventOnesList.Should().HaveCount(1);
        testEventOnesList[0].Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public async Task PushAsync_Messages_ReturnedAfterMessagesProcessed()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<IEvent> stream = provider.CreateStream<IEvent>();

        bool processed = false;

        Task.Run(
            async () =>
            {
                await Task.Delay(50);
                using IEnumerator<IEvent> enumerator = stream.GetEnumerator();
                enumerator.MoveNext();
                await Task.Delay(50);
                processed = true;
                using IEnumerator<IEvent> enumerator2 = stream.GetEnumerator();
                enumerator2.MoveNext();
            }).FireAndForget();

        await provider.PushAsync(new TestEventOne());

        processed.Should().BeTrue();
    }

    [Fact]
    public async Task PushAsync_MessageWithoutMatchingStream_ExceptionThrown()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<TestEventOne> stream = provider.CreateStream<TestEventOne>();

        Task.Run(() => stream.ToList()).FireAndForget();

        await provider.PushAsync(new TestEventOne());
        Func<Task> act = () => provider.PushAsync(new TestEventTwo());

        await act.Should().ThrowAsync<UnhandledMessageException>();
    }

    [Fact]
    public async Task PushAsync_MessageWithoutMatchingStreamDisablingException_NoExceptionThrown()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<TestEventOne> stream = provider.CreateStream<TestEventOne>();

        Task.Run(() => stream.ToList()).FireAndForget();

        await provider.PushAsync(new TestEventOne());
        Func<Task> act = () => provider.PushAsync(new TestEventTwo(), false);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PushAsync_Envelopes_RelayedToStream()
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

        envelopes.Should().HaveCount(3);
    }

    [Fact]
    public async Task PushAsync_Envelopes_UnwrappedAndRelayedToMatchingStreams()
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

        eventsList.Should().HaveCount(2);
        eventsList[0].Should().BeOfType<TestEventOne>();
        eventsList[1].Should().BeOfType<TestEventTwo>();

        testEventOnesList.Should().HaveCount(1);
        testEventOnesList[0].Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public async Task PushAsync_EnvelopeWithoutMatchingStream_ExceptionThrown()
    {
        MessageStreamProvider<IEnvelope> provider = new();
        IMessageStreamEnumerable<TestEventOne> stream = provider.CreateStream<TestEventOne>();

        Task.Run(() => stream.ToList()).FireAndForget();

        await provider.PushAsync(new TestEnvelope(new TestEventOne()));
        Func<Task> act = () => provider.PushAsync(new TestEnvelope(new TestEventTwo()));

        await act.Should().ThrowAsync<UnhandledMessageException>();
    }

    [Fact]
    public async Task PushAsync_WhileEnumeratingStream_BackpressureIsHandled()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();
        using IEnumerator<int> enumerator = stream.GetEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        Task<int> pushTask3 = provider.PushAsync(3);

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
    public async Task PushAsync_WhileAsyncEnumeratingStream_BackpressureIsHandled()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();
        await using IAsyncEnumerator<int> enumerator = stream.GetAsyncEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        Task<int> pushTask3 = provider.PushAsync(3);

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
    public async Task PushAsync_WhileEnumeratingMultipleStreams_BackpressureIsHandled()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream1 = provider.CreateStream<int>();
        IMessageStreamEnumerable<int> stream2 = provider.CreateStream<int>();
        using IEnumerator<int> enumerator1 = stream1.GetEnumerator();
        using IEnumerator<int> enumerator2 = stream2.GetEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        provider.PushAsync(3).FireAndForget();

        enumerator1.MoveNext();
        enumerator2.MoveNext();

        await Task.Delay(100);
        pushTask1.IsCompleted.Should().BeFalse();

        enumerator1.MoveNext();

        await Task.Delay(100);
        pushTask1.IsCompleted.Should().BeFalse();

        enumerator2.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.Should().BeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.Should().BeFalse();

        enumerator1.MoveNext();

        await Task.Delay(100);
        pushTask2.IsCompleted.Should().BeFalse();

        enumerator2.MoveNext();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task PushAsync_WhileAsyncEnumeratingMultipleStreams_BackpressureIsHandled()
    {
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream1 = provider.CreateStream<int>();
        IMessageStreamEnumerable<int> stream2 = provider.CreateStream<int>();
        await using IAsyncEnumerator<int> enumerator1 = stream1.GetAsyncEnumerator();
        await using IAsyncEnumerator<int> enumerator2 = stream2.GetAsyncEnumerator();

        Task<int> pushTask1 = provider.PushAsync(1);
        Task<int> pushTask2 = provider.PushAsync(2);
        provider.PushAsync(3).FireAndForget();

        await enumerator1.MoveNextAsync();
        await enumerator2.MoveNextAsync();

        await Task.Delay(100);
        pushTask1.IsCompleted.Should().BeFalse();

        await enumerator1.MoveNextAsync();

        await Task.Delay(100);
        pushTask1.IsCompleted.Should().BeFalse();

        await enumerator2.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask1.IsCompleted);
        pushTask1.IsCompleted.Should().BeTrue();

        await Task.Delay(100);
        pushTask2.IsCompleted.Should().BeFalse();

        await enumerator1.MoveNextAsync();

        await Task.Delay(100);
        pushTask2.IsCompleted.Should().BeFalse();

        await enumerator2.MoveNextAsync();

        await AsyncTestingUtil.WaitAsync(() => pushTask2.IsCompleted);
        pushTask2.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteAsync_WhileEnumeratingStream_EnumerationCompleted()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(
            () =>
            {
                count = stream.Count();
            });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.Should().BeNull();

        await provider.CompleteAsync();
        await enumerationTask;

        count.Should().Be(3);
    }

    [Fact]
    public async Task CompleteAsync_WhileAsyncEnumeratingStreams_EnumerationCompleted()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(
            async () =>
            {
                count = await stream.CountAsync();
            });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.Should().BeNull();

        await provider.CompleteAsync();
        await enumerationTask;

        count.Should().Be(3);
    }

    [Fact]
    public async Task Abort_WhileEnumeratingStream_EnumerationAborted()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(
            () =>
            {
                count = stream.Count();
            });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.Should().BeNull();

        provider.Abort();

        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        count.Should().BeNull();
        enumerationTask.Status.Should().Be(TaskStatus.Faulted);
    }

    [Fact]
    public async Task Abort_WhileAsyncEnumeratingStreams_EnumerationAborted()
    {
        int? count = null;
        MessageStreamProvider<int> provider = new();
        IMessageStreamEnumerable<int> stream = provider.CreateStream<int>();

        Task enumerationTask = Task.Run(
            async () =>
            {
                count = await stream.CountAsync();
            });

        await provider.PushAsync(1);
        await provider.PushAsync(2);
        await provider.PushAsync(3);

        count.Should().BeNull();

        provider.Abort();

        await AsyncTestingUtil.WaitAsync(() => enumerationTask.IsCompleted);

        count.Should().BeNull();
        enumerationTask.Status.Should().Be(TaskStatus.Canceled);
    }

    [Fact]
    public async Task CreateLazyStream_PushingMessages_StreamCreatedWhenMatchingMessagePushed()
    {
        MessageStreamProvider<IEvent> provider = new();
        ILazyMessageStreamEnumerable<TestEventTwo> lazyStream = provider.CreateLazyStream<TestEventTwo>();
        List<TestEventTwo> receivedTwos = [];
        bool completed = false;

        Task.Run(
            async () =>
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

        createStreamTask.Status.Should().NotBe(TaskStatus.RanToCompletion);
        lazyStream.Stream.Should().BeNull();

        await provider.PushAsync(new TestEventTwo());

        createStreamTask.Status.Should().Be(TaskStatus.RanToCompletion);
        lazyStream.Stream.Should().BeOfType<MessageStreamEnumerable<TestEventTwo>>();

        await provider.PushAsync(new TestEventTwo());

        receivedTwos.Should().HaveCount(2);

        await provider.CompleteAsync();

        await AsyncTestingUtil.WaitAsync(() => completed);
        completed.Should().BeTrue();
    }

    [Fact]
    public void CreateLazyStream_CalledTwiceForSameType_NewInstanceReturned()
    {
        MessageStreamProvider<IMessage> provider = new();
        ILazyMessageStreamEnumerable<IEvent> stream1 = provider.CreateLazyStream<IEvent>();
        ILazyMessageStreamEnumerable<IEvent> stream2 = provider.CreateLazyStream<IEvent>();

        stream2.Should().NotBeSameAs(stream1);
    }

    [Fact]
    public void CreateLazyStream_GenericAndNonGenericVersions_EquivalentInstanceReturned()
    {
        MessageStreamProvider<IMessage> provider = new();

        ILazyMessageStreamEnumerable<IEvent> stream1 = provider.CreateLazyStream<IEvent>();
        ILazyMessageStreamEnumerable<object> stream2 = provider.CreateLazyStream(typeof(IEvent));

        stream2.Should().BeOfType(stream1.GetType());
    }

    [Fact]
    public void CreateStream_CalledTwiceForSameType_NewInstanceReturned()
    {
        MessageStreamProvider<IMessage> provider = new();
        IMessageStreamEnumerable<IEvent> stream1 = provider.CreateStream<IEvent>();
        IMessageStreamEnumerable<IEvent> stream2 = provider.CreateStream<IEvent>();

        stream2.Should().NotBeSameAs(stream1);
    }

    [Fact]
    public void CreateStream_GenericAndNonGenericVersions_EquivalentInstanceReturned()
    {
        MessageStreamProvider<IMessage> provider = new();

        IMessageStreamEnumerable<IEvent> stream1 = provider.CreateStream<IEvent>();
        IMessageStreamEnumerable<object> stream2 = provider.CreateStream(typeof(IEvent));

        stream2.Should().BeOfType(stream1.GetType());
    }

    private class TestEvent : IEvent;

    private class TestEventOne : TestEvent;

    private class TestEventTwo : TestEvent;

    private class TestCommandOne : IMessage;

    private class TestEnvelope : IEnvelope
    {
        public TestEnvelope(object? message)
        {
            Message = message;
        }

        public bool AutoUnwrap => true;

        public object? Message { get; }

        public Type MessageType => Message?.GetType() ?? typeof(object);
    }
}
