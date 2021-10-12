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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Rx.TestTypes.Messages;
using Silverback.Tests.Core.Rx.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Rx.Messaging.Publishing
{
    public class StreamPublisherTests
    {
        [Fact]
        public async Task Publish_MessageStreamProvider_StreamedMessagesReceived()
        {
            var receivedStreams = 0;
            var receivedEvents = 0;
            var receivedTestEventOnes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref receivedEvents));
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEventOnes));
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider1 = new MessageStreamProvider<IEvent>();
            var streamProvider2 = new MessageStreamProvider<IEvent>();

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
            var receivedEnumeratedStreams = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IReadOnlyCollection<IEvent> _) =>
                        {
                            Interlocked.Increment(ref receivedEnumeratedStreams);
                        })
                    .AddDelegateSubscriber(
                        (List<TestEventOne> _) => { Interlocked.Increment(ref receivedEnumeratedStreams); }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEvent>();

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
            var receivedStreams = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => { });
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => { });
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());

            await Task.Delay(200);

            receivedStreams.Should().Be(0);
        }

        [Fact]
        public async Task Publish_MessageStreamProvider_OnlyRequiredStreamsPublished()
        {
            var receivedStreams = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => { });
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => { });
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestCommandOne> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => { });
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider1 = new MessageStreamProvider<IEvent>();
            var streamProvider2 = new MessageStreamProvider<IEvent>();

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
            var receivedStreams = 0;
            var received = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(
                                _ =>
                                {
                                    if (Interlocked.Increment(ref received) >= 3)
                                        throw new TestException();
                                });
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref received));
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEvent>();

            var tasks = streamPublisher.Publish(streamProvider);

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
            var received = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            observable.Subscribe(
                                _ =>
                                {
                                    if (Interlocked.Increment(ref received) >= 3)
                                        throw new TestException();
                                });
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            observable.Subscribe(_ => { });
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEvent>();

            var tasks = await streamPublisher.PublishAsync(streamProvider);

            streamProvider.PushAsync(new TestEventOne()).FireAndForget();
            streamProvider.PushAsync(new TestEventOne()).FireAndForget();
            streamProvider.PushAsync(new TestEventOne()).FireAndForget();

            Func<Task> act = async () => await await Task.WhenAny(tasks);

            await act.Should().ThrowAsync<TargetInvocationException>();
        }

        [Fact]
        public async Task PublishAsync_MessageStreamProviderAbortedAfterException_AllPendingTasksCompleted()
        {
            var received = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            observable.Subscribe(
                                _ =>
                                {
                                    if (Interlocked.Increment(ref received) >= 3)
                                        throw new TestException();
                                });
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            observable.Subscribe(_ => { });
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEvent>();

            var tasks = await streamPublisher.PublishAsync(streamProvider);

            streamProvider.PushAsync(new TestEventOne()).FireAndForget();
            streamProvider.PushAsync(new TestEventOne()).FireAndForget();
            streamProvider.PushAsync(new TestEventOne()).FireAndForget();

            var whenAnyTask = await Task.WhenAny(tasks);

            whenAnyTask.Status.Should().Be(TaskStatus.Faulted);

            streamProvider.Abort();

            await AsyncTestingUtil.WaitAsync(() => tasks.All(task => task.IsCompleted));
            tasks.All(task => task.IsCompleted).Should().BeTrue();
        }

        [Fact]
        public async Task Publish_MessageStreamProviderOfEnvelopes_StreamedEnvelopesReceived()
        {
            var receivedStreams = 0;
            var receivedEnvelopes = 0;
            var receivedTestEnvelopes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEnvelope> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref receivedEnvelopes));
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEnvelope> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEnvelopes));
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEnvelope>();

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
            var receivedStreams = 0;
            var receivedTestEventOnes = 0;
            var receivedTestEnvelopes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEventOnes));
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEnvelope> observable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            observable.Subscribe(_ => Interlocked.Increment(ref receivedTestEnvelopes));
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var stream = new MessageStreamProvider<IEnvelope>();
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
            var receivedEvents = new List<IEvent>();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AsObservable()
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<IEvent> observable) =>
                        {
                            observable.Subscribe(message => receivedEvents.Add(message));
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEnvelope>();
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
}
