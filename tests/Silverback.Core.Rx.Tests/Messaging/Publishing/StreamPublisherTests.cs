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
using Silverback.Tests.Core.Rx.TestTypes.Messages;
using Silverback.Tests.Core.Rx.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging.Publishing;

public class StreamPublisherTests
{
    [Fact]
    public async Task Publish_MessageStreamProvider_StreamedMessagesReceived()
    {
        int receivedStreams = 0;
        int receivedEvents = 0;
        int receivedTestEventOnes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamObservable<IEvent> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref receivedEvents));
        }

        void Handle2(IMessageStreamObservable<TestEventOne> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEventOnes));
        }

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
    public async Task Publish_MessageStreamProvider_MessagesNotAutomaticallyEnumerated()
    {
        int receivedEnumeratedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IReadOnlyCollection<IEvent>>(Handle1)
                .AddDelegateSubscriber2<List<TestEventOne>>(Handle2));

        void Handle1(IReadOnlyCollection<IEvent> message) => Interlocked.Increment(ref receivedEnumeratedStreams);
        void Handle2(List<TestEventOne> message) => Interlocked.Increment(ref receivedEnumeratedStreams);

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEventOne(), false);
        await streamProvider.PushAsync(new TestEventTwo(), false);

        await streamProvider.CompleteAsync();

        await AsyncTestingUtil.WaitAsync(
            () => receivedEnumeratedStreams >= 1,
            TimeSpan.FromMilliseconds(500));
        receivedEnumeratedStreams.Should().Be(0);
    }

    [Fact]
    public async Task Publish_SimpleMessageAndObservableOfMessages_StreamSubscribersNotInvoked()
    {
        int receivedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamObservable<IEvent> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(
                _ =>
                {
                });
        }

        void Handle2(IMessageStreamObservable<TestEventOne> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(
                _ =>
                {
                });
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        await Task.Delay(200);

        receivedStreams.Should().Be(0);
    }

    [Fact]
    public async Task Publish_MessageStreamProvider_OnlyRequiredStreamsPublished()
    {
        int receivedStreams = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle2)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestCommandOne>>(Handle3));

        void Handle1(IMessageStreamObservable<IEvent> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(
                _ =>
                {
                });
        }

        void Handle2(IMessageStreamObservable<TestEventOne> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(
                _ =>
                {
                });
        }

        void Handle3(IMessageStreamObservable<TestCommandOne> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(
                _ =>
                {
                });
        }

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
    public async Task Publish_MessageStreamProvider_ProcessExceptionRethrown()
    {
        int receivedStreams = 0;
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamObservable<IEvent> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(
                _ =>
                {
                    if (Interlocked.Increment(ref received) >= 3)
                        throw new TestException();
                });
        }

        void Handle2(IMessageStreamObservable<TestEventOne> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref received));
        }

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEvent> streamProvider = new();

        IReadOnlyCollection<Task> tasks = streamPublisher.Publish(streamProvider);

        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();
        streamProvider.PushAsync(new TestEventOne()).FireAndForget();

        await AsyncTestingUtil.WaitAsync(() => received >= 3);

        Func<Task> act = async () => await await Task.WhenAny(tasks);

        await act.Should().ThrowAsync<TargetInvocationException>();
    }

    [Fact]
    public async Task PublishAsync_MessageStreamProvider_ProcessExceptionRethrown()
    {
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamObservable<IEvent> observable) =>
            observable.Subscribe(
                _ =>
                {
                    if (Interlocked.Increment(ref received) >= 3)
                        throw new TestException();
                });

        static void Handle2(IMessageStreamObservable<TestEventOne> observable) =>
            observable.Subscribe(
                _ =>
                {
                });

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
    public async Task PublishAsync_MessageStreamProviderAbortedAfterException_AllPendingTasksCompleted()
    {
        int received = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle2));

        void Handle1(IMessageStreamObservable<IEvent> observable) =>
            observable.Subscribe(
                _ =>
                {
                    if (Interlocked.Increment(ref received) >= 3)
                        throw new TestException();
                });

        static void Handle2(IMessageStreamObservable<TestEventOne> observable) =>
            observable.Subscribe(
                _ =>
                {
                });

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
    public async Task Publish_MessageStreamProviderOfEnvelopes_StreamedEnvelopesReceived()
    {
        int receivedStreams = 0;
        int receivedEnvelopes = 0;
        int receivedTestEnvelopes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEnvelope>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEnvelope>>(Handle2));

        void Handle1(IMessageStreamObservable<IEnvelope> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref receivedEnvelopes));
        }

        void Handle2(IMessageStreamObservable<TestEnvelope> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEnvelopes));
        }

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
    public async Task Publish_MessageStreamProviderOfEnvelopes_StreamedUnwrappedMessagesReceived()
    {
        int receivedStreams = 0;
        int receivedTestEventOnes = 0;
        int receivedTestEnvelopes = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEventOne>>(Handle1)
                .AddDelegateSubscriber2<IMessageStreamObservable<TestEnvelope>>(Handle2));

        void Handle1(IMessageStreamObservable<TestEventOne> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEventOnes));
        }

        void Handle2(IMessageStreamObservable<TestEnvelope> observable)
        {
            Interlocked.Increment(ref receivedStreams);
            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEnvelopes));
        }

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
    public async Task Publish_MessageStreamProviderOfEnvelopes_OnlyAutoUnwrapMessagesReceived()
    {
        List<IEvent> receivedEvents = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AsObservable()
                .AddDelegateSubscriber2<IMessageStreamObservable<IEvent>>(Handle));

        void Handle(IMessageStreamObservable<IEvent> observable) =>
            observable.Subscribe(message => receivedEvents.Add(message));

        IStreamPublisher streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

        MessageStreamProvider<IEnvelope> streamProvider = new();
        await streamPublisher.PublishAsync(streamProvider);

        await streamProvider.PushAsync(new TestEnvelope(new TestEventOne(), false), false);
        await streamProvider.PushAsync(new TestEnvelope(new TestEventTwo()));

        await AsyncTestingUtil.WaitAsync(() => receivedEvents.Count >= 1);

        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].Should().BeOfType<TestEventTwo>();
    }

    // TODO
    // * Test behaviors?
}
