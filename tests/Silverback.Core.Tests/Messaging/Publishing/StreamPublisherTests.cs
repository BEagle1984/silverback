// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public class StreamPublisherTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private interface IEvent : IMessage;

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribers_WhenMessageStreamProviderIsPushedWithMessages()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (IEvent dummy in enumerable)
            {
                Interlocked.Increment(ref receivedEvents);
            }
        }

        async Task Handle2(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (TestEventOne dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEventOnes);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        IReadOnlyCollection<Task> tasks1 = streamPublisher.Publish(streamProvider1);
        IReadOnlyCollection<Task> tasks2 = await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventOne());
        await streamProvider2.PushAsync(new TestEventOne());
        await streamProvider1.PushAsync(new TestEventTwo());
        await streamProvider2.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

        receivedStreams.ShouldBe(4);
        receivedEvents.ShouldBe(4);
        receivedTestEventOnes.ShouldBe(2);

        await CompleteAndWaitForSubscribersAsync(streamProvider1, tasks1);
        await CompleteAndWaitForSubscribersAsync(streamProvider2, tasks2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribersExpectingEnumerable_WhenMessageStreamProviderIsPushedWithMessages()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IEnumerable<TestEventOne>>(Handle2));

        void Handle1(IEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (IEvent dummy in enumerable)
            {
                Interlocked.Increment(ref receivedEvents);
            }
        }

        void Handle2(IEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (TestEventOne dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEventOnes);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        IReadOnlyCollection<Task> tasks1 = streamPublisher.Publish(streamProvider1);
        IReadOnlyCollection<Task> tasks2 = await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventOne());
        await streamProvider2.PushAsync(new TestEventOne());
        await streamProvider1.PushAsync(new TestEventTwo());
        await streamProvider2.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

        receivedStreams.ShouldBe(4);
        receivedEvents.ShouldBe(4);
        receivedTestEventOnes.ShouldBe(2);

        await CompleteAndWaitForSubscribersAsync(streamProvider1, tasks1);
        await CompleteAndWaitForSubscribersAsync(streamProvider2, tasks2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribersExpectingAsyncEnumerable_WhenMessageStreamProviderIsPushedWithMessages()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IAsyncEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(Handle2));

        async Task Handle1(IAsyncEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (IEvent dummy in enumerable)
            {
                Interlocked.Increment(ref receivedEvents);
            }
        }

        async ValueTask Handle2(IAsyncEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (TestEventOne dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEventOnes);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        IReadOnlyCollection<Task> tasks1 = streamPublisher.Publish(streamProvider1);
        IReadOnlyCollection<Task> tasks2 = await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventOne());
        await streamProvider2.PushAsync(new TestEventOne());
        await streamProvider1.PushAsync(new TestEventTwo());
        await streamProvider2.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

        receivedStreams.ShouldBe(4);
        receivedEvents.ShouldBe(4);
        receivedTestEventOnes.ShouldBe(2);

        await CompleteAndWaitForSubscribersAsync(streamProvider1, tasks1);
        await CompleteAndWaitForSubscribersAsync(streamProvider2, tasks2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotAutomaticallyEnumerate_WhenMessageStreamProviderIsPublished()
    {
        int receivedEnumeratedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IReadOnlyCollection<IEvent>>(Handle1)
            .AddDelegateSubscriber<List<TestEventOne>>(Handle2));

        void Handle1(IReadOnlyCollection<IEvent> messages) => Interlocked.Increment(ref receivedEnumeratedStreams);
        void Handle2(List<TestEventOne> messages) => Interlocked.Increment(ref receivedEnumeratedStreams);

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();
        MessageStreamProvider<IEvent> streamProvider = new();

        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEventOne(), false);
        await streamProvider.PushAsync(new TestEventTwo(), false);

        await streamProvider.CompleteAsync();

        await AsyncTestingUtil.WaitAsync(() => receivedEnumeratedStreams >= 1, TimeSpan.FromMilliseconds(500));
        receivedEnumeratedStreams.ShouldBe(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotInvokeMessageStreamEnumerableSubscribers_WhenSimpleMessageIsPublished()
    {
        int receivedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (IEvent dummy in enumerable)
            {
                // Irrelevant
            }
        }

        async Task Handle2(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (TestEventOne dummy in enumerable)
            {
                // Irrelevant
            }
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        await Task.Delay(200);

        receivedStreams.ShouldBe(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeMatchingSubscribersOnly_WhenMessageStreamIsPublished()
    {
        int receivedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle2)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestCommandOne>>(Handle3));

        void Handle1(IMessageStreamEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            List<IEvent> dummy = [.. enumerable];
        }

        void Handle2(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            List<TestEventOne> dummy = [.. enumerable];
        }

        void Handle3(IMessageStreamEnumerable<TestCommandOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            List<TestCommandOne> dummy = [.. enumerable];
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        await Task.Delay(100);
        receivedStreams.ShouldBe(0);

        IReadOnlyCollection<Task> tasks1 = streamPublisher.Publish(streamProvider1);
        IReadOnlyCollection<Task> tasks2 = await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventTwo());
        await Task.Delay(100);

        receivedStreams.ShouldBe(1);

        await streamProvider1.PushAsync(new TestEventOne());
        await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 2);

        receivedStreams.ShouldBe(2);

        await streamProvider2.PushAsync(new TestEventOne());
        await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 4);

        receivedStreams.ShouldBe(4);

        await CompleteAndWaitForSubscribersAsync(streamProvider1, tasks1);
        await CompleteAndWaitForSubscribersAsync(streamProvider2, tasks2);
    }

    [Fact]
    public async Task Publish_ShouldRethrow_WhenMessageStreamSubscriberThrows()
    {
        int receivedStreams = 0;
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (IEvent dummy in enumerable)
            {
                if (Interlocked.Increment(ref received) >= 3)
                    throw new TestException();
            }
        }

        async Task Handle2(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (TestEventOne dummy in enumerable)
            {
                Interlocked.Increment(ref received);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = streamPublisher.Publish(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        await AsyncTestingUtil.WaitAsync(() => received >= 5);

        Func<Task> act = async () => await await Task.WhenAny(tasks);

        await act.ShouldThrowAsync<TestException>();

        streamProvider.Abort();

        await AsyncTestingUtil.WaitAsync(() => tasks.All(task => task.IsCompleted));
        tasks.All(task => task.IsCompleted).ShouldBeTrue();
    }

    [Fact]
    public async Task PublishAsync_ShouldRethrow_WhenMessageStreamSubscriberThrows()
    {
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamEnumerable<IEvent> enumerable)
        {
            foreach (IEvent dummy in enumerable)
            {
                if (Interlocked.Increment(ref received) >= 3)
                    throw new TestException();
            }
        }

        static async Task Handle2(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne dummy in enumerable)
            {
                // Irrelevant
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        Func<Task> act = async () => await await Task.WhenAny(tasks);

        await act.ShouldThrowAsync<TestException>();

        streamProvider.Abort();

        await AsyncTestingUtil.WaitAsync(() => tasks.All(task => task.IsCompleted));
        tasks.All(task => task.IsCompleted).ShouldBeTrue();
    }

    [Fact]
    public async Task PublishAsync_ShouldCompleteStreamProvider_WhenMessageStreamSubscriberThrows()
    {
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamEnumerable<IEvent> enumerable)
        {
            foreach (IEvent dummy in enumerable)
            {
                if (Interlocked.Increment(ref received) >= 3)
                    throw new TestException();
            }
        }

        static async Task Handle2(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne dummy in enumerable)
            {
                // Irrelevant
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        Task whenAnyTask = await Task.WhenAny(tasks);

        whenAnyTask.Status.ShouldBe(TaskStatus.Faulted);

        streamProvider.Abort();

        await AsyncTestingUtil.WaitAsync(() => tasks.All(task => task.IsCompleted));
        tasks.All(task => task.IsCompleted).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The method waits for the subscribers to complete.")]
    public async Task PublishAsync_ShouldEndStreamsOnlyWhenTokenIsCanceled_WhenStreamSubscriberThrowsCancellationWhileOtherIsEnumerating(
        bool isTokenCanceled)
    {
        using CancellationTokenSource cancellationTokenSource = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(Handle1)
            .AddDelegateSubscriber<IAsyncEnumerable<IEvent>>(Handle2));

        async Task Handle1(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne dummy in enumerable)
            {
                if (isTokenCanceled)
                    await cancellationTokenSource.CancelAsync();

                throw new OperationCanceledException();
            }
        }

        static async Task Handle2(IAsyncEnumerable<IEvent> enumerable)
        {
            await foreach (IEvent dummy in enumerable)
            {
                // Keep enumerating
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();
        MessageStreamProvider<IEvent> streamProvider = new(detachesReturningSubscribers: true);

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider, cancellationTokenSource.Token);
        Task<int> pushTask = streamProvider.PushAsync(new TestEventOne());

        // The subscriber is notified as completed before its task completes
        await Task.WhenAny(tasks).WaitAsync(Timeout);

        // The cancellation requested through the token ends the streams, while any other cancellation is a failure, left to its abort
        streamProvider.SubscribersReturnedTask.IsCompleted.ShouldBe(isTokenCanceled);

        streamProvider.Abort();
        await Should.ThrowAsync<OperationCanceledException>(pushTask.WaitAsync(Timeout));
        await AsyncTestingUtil.WaitAsync(() => tasks.All(task => task.IsCompleted));
        tasks.All(task => task.IsCompleted).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task PublishAsync_ShouldCompleteStream_WhenSubscriberStopsWhileCompleting(bool pullMessage, bool throwException)
    {
        TaskCompletionSource invokedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stopTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(Handle));

        // Return or throw after pulling the first message (like breaking out of the enumeration) or without pulling it
        async Task Handle(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await using IAsyncEnumerator<TestEventOne> enumerator = enumerable.GetAsyncEnumerator();

            if (pullMessage)
                await enumerator.MoveNextAsync();

            invokedTaskCompletionSource.SetResult();
            await stopTaskCompletionSource.Task;

            if (throwException)
                throw new TestException();
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        // The subscriber is invoked as soon as the first message creates the stream
        Task<int> pushTask = streamProvider.PushAsync(new TestEventOne());
        await invokedTaskCompletionSource.Task.WaitAsync(Timeout);

        // The completion waits for the pending push, which completes only when the next message is pulled
        Task completeTask = streamProvider.CompleteAsync().AsTask();
        await Task.WhenAny(completeTask, Task.Delay(100));
        completeTask.IsCompleted.ShouldBeFalse();

        stopTaskCompletionSource.SetResult();

        await completeTask.WaitAsync(Timeout);
        (await pushTask.WaitAsync(Timeout)).ShouldBe(1);
        Exception? processingException = await Record.ExceptionAsync(() => tasks.Single().WaitAsync(Timeout));

        if (throwException)
            processingException.ShouldBeOfType<TestException>();
        else
            processingException.ShouldBeNull();
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribers_WhenStreamProviderOfEnvelopesIsPublished()
    {
        int receivedStreams = 0;
        int receivedEnvelopes = 0;
        int receivedTestEnvelopes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEnvelope>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEnvelope>>(Handle2));

        void Handle1(IMessageStreamEnumerable<IEnvelope> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (IEnvelope dummy in enumerable)
            {
                Interlocked.Increment(ref receivedEnvelopes);
            }
        }

        async Task Handle2(IMessageStreamEnumerable<TestEnvelope> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (TestEnvelope dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEnvelopes);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEnvelope(new TestEventOne()));
        await streamProvider.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedEnvelopes >= 2 && receivedTestEnvelopes >= 2);

        receivedStreams.ShouldBe(2);
        receivedEnvelopes.ShouldBe(2);
        receivedTestEnvelopes.ShouldBe(2);

        await CompleteAndWaitForSubscribersAsync(streamProvider, tasks);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribersWithUnwrappedMessages_WhenStreamProviderOfEnvelopesIsPublished()
    {
        int receivedStreams = 0;
        int receivedTestEventOnes = 0;
        int receivedTestEnvelopes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEnvelope>>(Handle2));

        void Handle1(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (TestEventOne dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEventOnes);
            }
        }

        async Task Handle2(IMessageStreamEnumerable<TestEnvelope> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            await foreach (TestEnvelope dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEnvelopes);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> stream = new();
        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(stream);

        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedTestEventOnes >= 2 && receivedTestEnvelopes >= 4);

        receivedStreams.ShouldBe(2);
        receivedTestEventOnes.ShouldBe(2);
        receivedTestEnvelopes.ShouldBe(4);

        await CompleteAndWaitForSubscribersAsync(stream, tasks);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeMatchingUnwrappedMessageSubscribersOnly_WhenStreamProviderOfEnvelopesIsPublished()
    {
        int receivedStreamsOfOnes = 0;
        int receivedTestEventOnes = 0;
        int receivedStreamsOfTwos = 0;
        int receivedTestEventTwos = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(Handle1)
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventTwo>>(Handle2));

        void Handle1(IMessageStreamEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref receivedStreamsOfOnes);
            foreach (TestEventOne dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEventOnes);
            }
        }

        void Handle2(IMessageStreamEnumerable<TestEventTwo> enumerable)
        {
            Interlocked.Increment(ref receivedStreamsOfTwos);
            foreach (TestEventTwo dummy in enumerable)
            {
                Interlocked.Increment(ref receivedTestEventTwos);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> stream = new();
        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(stream);

        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedTestEventTwos >= 2);
        receivedStreamsOfOnes.ShouldBe(0);
        receivedTestEventOnes.ShouldBe(0);
        receivedStreamsOfTwos.ShouldBe(1);
        receivedTestEventTwos.ShouldBe(2);

        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedTestEventTwos >= 4);

        receivedStreamsOfOnes.ShouldBe(1);
        receivedTestEventOnes.ShouldBe(2);
        receivedStreamsOfTwos.ShouldBe(1);
        receivedTestEventTwos.ShouldBe(4);

        await CompleteAndWaitForSubscribersAsync(stream, tasks);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribers_WhenBehaviorsAreConfigured()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;

        TestBehavior testBehavior = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IEvent>>(Handle)
            .AddSingletonBehavior(testBehavior));

        void Handle(IMessageStreamEnumerable<IEvent> enumerable)
        {
            Interlocked.Increment(ref receivedStreams);
            foreach (IEvent dummy in enumerable)
            {
                Interlocked.Increment(ref receivedEvents);
            }
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEventOne());
        await streamProvider.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 2);

        receivedStreams.ShouldBe(1);
        receivedEvents.ShouldBe(2);
        testBehavior.EnterCount.ShouldBe(1);
        testBehavior.ExitCount.ShouldBe(1);

        await CompleteAndWaitForSubscribersAsync(streamProvider, tasks);
    }

    private static async Task CompleteAndWaitForSubscribersAsync(MessageStreamProvider streamProvider, IEnumerable<Task> subscriberTasks)
    {
        await streamProvider.CompleteAsync().AsTask().WaitAsync(Timeout);
        await Task.WhenAll(subscriberTasks).WaitAsync(Timeout);
    }

    private class TestEventOne : IEvent;

    private class TestEventTwo : IEvent;

    private class TestCommandOne : IMessage;

    private class TestEnvelope : IEnvelope
    {
        public TestEnvelope(object? message)
        {
            Message = message;
        }

        public object? Message { get; }

        public Type MessageType => Message?.GetType() ?? typeof(object);
    }

    private class TestBehavior : IBehavior
    {
        private readonly IList<string>? _calls;

        public TestBehavior(IList<string>? calls = null)
        {
            _calls = calls;
        }

        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            _calls?.Add("unsorted");

            EnterCount++;

            ValueTask<IReadOnlyCollection<object?>> result = next(message);

            ExitCount++;

            return result;
        }
    }
}
