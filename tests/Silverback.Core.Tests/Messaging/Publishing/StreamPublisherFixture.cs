// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public class StreamPublisherFixture
{
    private interface IEvent : IMessage
    {
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribers_WhenMessageStreamProviderIsPushedWithMessages()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (IEvent dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedEvents);
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (TestEventOne dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEventOnes);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        streamPublisher.Publish(streamProvider1);
        await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventOne());
        await streamProvider2.PushAsync(new TestEventOne());
        await streamProvider1.PushAsync(new TestEventTwo());
        await streamProvider2.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

        receivedStreams.Should().Be(4);
        receivedEvents.Should().Be(4);
        receivedTestEventOnes.Should().Be(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribersExpectingEnumerable_WhenMessageStreamProviderIsPushedWithMessages()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (IEvent dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedEvents);
                        }
                    })
                .AddDelegateSubscriber(
                    (IEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (TestEventOne dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEventOnes);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        streamPublisher.Publish(streamProvider1);
        await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventOne());
        await streamProvider2.PushAsync(new TestEventOne());
        await streamProvider1.PushAsync(new TestEventTwo());
        await streamProvider2.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

        receivedStreams.Should().Be(4);
        receivedEvents.Should().Be(4);
        receivedTestEventOnes.Should().Be(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeSubscribersExpectingAsyncEnumerable_WhenMessageStreamProviderIsPushedWithMessages()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    async (IAsyncEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (IEvent dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedEvents);
                        }
                    })
                .AddDelegateSubscriber(
                    async (IAsyncEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (TestEventOne dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEventOnes);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        streamPublisher.Publish(streamProvider1);
        await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventOne());
        await streamProvider2.PushAsync(new TestEventOne());
        await streamProvider1.PushAsync(new TestEventTwo());
        await streamProvider2.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

        receivedStreams.Should().Be(4);
        receivedEvents.Should().Be(4);
        receivedTestEventOnes.Should().Be(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotAutomaticallyEnumerate_WhenMessageStreamProviderIsPublished()
    {
        int receivedEnumeratedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IReadOnlyCollection<IEvent> _) =>
                    {
                        Interlocked.Increment(ref receivedEnumeratedStreams);
                    })
                .AddDelegateSubscriber(
                    (List<TestEventOne> _) =>
                    {
                        Interlocked.Increment(ref receivedEnumeratedStreams);
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();
        MessageStreamProvider<IEvent> streamProvider = new();

        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEventOne(), false);
        await streamProvider.PushAsync(new TestEventTwo(), false);

        await streamProvider.CompleteAsync();

        await AsyncTestingUtil.WaitAsync(() => receivedEnumeratedStreams >= 1, TimeSpan.FromMilliseconds(500));
        receivedEnumeratedStreams.Should().Be(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotInvokeMessageStreamEnumerableSubscribers_WhenSimpleMessageIsPublished()
    {
        int receivedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (IEvent dummy in enumerable)
                        {
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (TestEventOne dummy in enumerable)
                        {
                        }
                    }));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        await Task.Delay(200);

        receivedStreams.Should().Be(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldInvokeMatchingSubscribersOnly_WhenMessageStreamIsPublished()
    {
        int receivedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        List<IEvent> dummy = enumerable.ToList();
                    })
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        List<TestEventOne> dummy = enumerable.ToList();
                    })
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestCommandOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        List<TestCommandOne> dummy = enumerable.ToList();
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider1 = new();
        MessageStreamProvider<IEvent> streamProvider2 = new();

        await Task.Delay(100);
        receivedStreams.Should().Be(0);

        streamPublisher.Publish(streamProvider1);
        await streamPublisher.PublishAsync(streamProvider2);

        await streamProvider1.PushAsync(new TestEventTwo());
        await Task.Delay(100);

        receivedStreams.Should().Be(1);

        await streamProvider1.PushAsync(new TestEventOne());
        await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 2);

        receivedStreams.Should().Be(2);

        await streamProvider2.PushAsync(new TestEventOne());
        await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 4);

        receivedStreams.Should().Be(4);
    }

    [Fact]
    public async Task Publish_ShouldRethrow_WhenMessageStreamSubscriberThrows()
    {
        int receivedStreams = 0;
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (IEvent dummy in enumerable)
                        {
                            if (Interlocked.Increment(ref received) >= 3)
                                throw new TestException();
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (TestEventOne dummy in enumerable)
                        {
                            Interlocked.Increment(ref received);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = streamPublisher.Publish(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        await AsyncTestingUtil.WaitAsync(() => received >= 5);

        Func<Task> act = async () => await await Task.WhenAny(tasks);

        await act.Should().ThrowAsync<TargetInvocationException>();
    }

    [Fact]
    public async Task PublishAsync_ShouldRethrow_WhenMessageStreamSubscriberThrows()
    {
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        foreach (IEvent dummy in enumerable)
                        {
                            if (Interlocked.Increment(ref received) >= 3)
                                throw new TestException();
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        await foreach (TestEventOne dummy in enumerable)
                        {
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        Func<Task> act = async () => await await Task.WhenAny(tasks);

        await act.Should().ThrowAsync<TargetInvocationException>();
    }

    [Fact]
    public async Task PublishAsync_ShouldCompleteStreamProvider_WhenMessageStreamSubscriberThrows()
    {
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        foreach (IEvent dummy in enumerable)
                        {
                            if (Interlocked.Increment(ref received) >= 3)
                                throw new TestException();
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        await foreach (TestEventOne dummy in enumerable)
                        {
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = await streamPublisher.PublishAsync(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        Task whenAnyTask = await Task.WhenAny(tasks);

        whenAnyTask.Status.Should().Be(TaskStatus.Faulted);

        streamProvider.Abort();

        await AsyncTestingUtil.WaitAsync(() => tasks.All(task => task.IsCompleted));
        tasks.All(task => task.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribers_WhenStreamProviderOfEnvelopesIsPublished()
    {
        int receivedStreams = 0;
        int receivedEnvelopes = 0;
        int receivedTestEnvelopes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEnvelope> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (IEnvelope dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedEnvelopes);
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEnvelope> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (TestEnvelope dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEnvelopes);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> streamProvider = new();

        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEnvelope(new TestEventOne()));
        await streamProvider.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedEnvelopes >= 2 && receivedTestEnvelopes >= 2);

        receivedStreams.Should().Be(2);
        receivedEnvelopes.Should().Be(2);
        receivedTestEnvelopes.Should().Be(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribersWithUnwrappedMessages_WhenStreamProviderOfEnvelopesIsPublished()
    {
        int receivedStreams = 0;
        int receivedTestEventOnes = 0;
        int receivedTestEnvelopes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (TestEventOne dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEventOnes);
                        }
                    })
                .AddDelegateSubscriber(
                    async (IMessageStreamEnumerable<TestEnvelope> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        await foreach (TestEnvelope dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEnvelopes);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> stream = new();
        await streamPublisher.PublishAsync(stream);

        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedTestEventOnes >= 2 && receivedTestEnvelopes >= 4);

        receivedStreams.Should().Be(2);
        receivedTestEventOnes.Should().Be(2);
        receivedTestEnvelopes.Should().Be(4);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeMatchingUnwrappedMessageSubscribersOnly_WhenStreamProviderOfEnvelopesIsPublished()
    {
        int receivedStreamsOfOnes = 0;
        int receivedTestEventOnes = 0;
        int receivedStreamsOfTwos = 0;
        int receivedTestEventTwos = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreamsOfOnes);
                        foreach (TestEventOne dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEventOnes);
                        }
                    })
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<TestEventTwo> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreamsOfTwos);
                        foreach (TestEventTwo dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedTestEventTwos);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> stream = new();
        await streamPublisher.PublishAsync(stream);

        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedTestEventTwos >= 2);
        receivedStreamsOfOnes.Should().Be(0);
        receivedTestEventOnes.Should().Be(0);
        receivedStreamsOfTwos.Should().Be(1);
        receivedTestEventTwos.Should().Be(2);

        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
        await stream.PushAsync(new TestEnvelope(new TestEventOne()));
        await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedTestEventTwos >= 4);

        receivedStreamsOfOnes.Should().Be(1);
        receivedTestEventOnes.Should().Be(2);
        receivedStreamsOfTwos.Should().Be(1);
        receivedTestEventTwos.Should().Be(4);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeUnwrappedMessageSubscribersOnlyWhenEnvelopeIsAutoUnwrap()
    {
        List<IEvent> receivedEvents = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        foreach (IEvent envelope in enumerable)
                        {
                            receivedEvents.Add(envelope);
                        }
                    }));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> streamProvider = new();
        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEnvelope(new TestEventOne(), false), false);
        await streamProvider.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedEvents.Count >= 1);

        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeSubscribers_WhenBehaviorsAreConfigured()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;

        TestBehavior testBehavior = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber(
                    (IMessageStreamEnumerable<IEvent> enumerable) =>
                    {
                        Interlocked.Increment(ref receivedStreams);
                        foreach (IEvent dummy in enumerable)
                        {
                            Interlocked.Increment(ref receivedEvents);
                        }
                    })
                .AddSingletonBehavior(testBehavior));
        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEventOne());
        await streamProvider.PushAsync(new TestEventTwo());

        await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 2);

        receivedStreams.Should().Be(1);
        receivedEvents.Should().Be(2);
        testBehavior.EnterCount.Should().Be(1);
        testBehavior.ExitCount.Should().Be(1);
    }

    private class TestEventOne : IEvent
    {
        public string? Message { get; init; }
    }

    private class TestEventTwo : IEvent
    {
    }

    private class TestCommandOne : IMessage
    {
    }

    private class TestEnvelope : IEnvelope
    {
        public TestEnvelope(object? message, bool autoUnwrap = true)
        {
            Message = message;
            AutoUnwrap = autoUnwrap;
        }

        public bool AutoUnwrap { get; }

        public object? Message { get; set; }
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

        public Task<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
        {
            _calls?.Add("unsorted");

            EnterCount++;

            Task<IReadOnlyCollection<object?>> result = next(message);

            ExitCount++;

            return result;
        }
    }
}
