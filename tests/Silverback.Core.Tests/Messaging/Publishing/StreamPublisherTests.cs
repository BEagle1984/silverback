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
using Silverback.Tests.Core.TestTypes.Behaviors;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class StreamPublisherTests
    {
        [Fact]
        public async Task Publish_StreamProvider_MessagesReceived()
        {
            var receivedStreams = 0;
            var receivedEvents = 0;
            var receivedTestEventOnes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedEvents);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEventOnes);
                            }
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
        public async Task Publish_StreamProviderSubscribedAsEnumerable_MessagesReceived()
        {
            var receivedStreams = 0;
            var receivedEvents = 0;
            var receivedTestEventOnes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedEvents);
                            }
                        })
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEventOnes);
                            }
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
        public async Task Publish_StreamProviderSubscribedAsAsyncEnumerable_MessagesReceived()
        {
            var receivedStreams = 0;
            var receivedEvents = 0;
            var receivedTestEventOnes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedEvents);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEventOnes);
                            }
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
        public async Task Publish_StreamProvider_MessagesNotAutomaticallyEnumerated()
        {
            var receivedEnumeratedStreams = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
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
        public async Task Publish_SimpleMessageAndEnumerableOfMessages_StreamSubscribersNotInvoked()
        {
            var receivedStreams = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                            }
                        }));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());

            await Task.Delay(200);

            receivedStreams.Should().Be(0);
        }

        [Fact]
        public async Task Publish_StreamProvider_OnlyRequiredStreamsPublished()
        {
            var receivedStreams = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            var dummy = enumerable.ToList();
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            var dummy = enumerable.ToList();
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestCommandOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            var dummy = enumerable.ToList();
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
        public async Task Publish_StreamProvider_ProcessExceptionRethrown()
        {
            var receivedStreams = 0;
            var received = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                if (Interlocked.Increment(ref received) >= 3)
                                    throw new TestException();
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref received);
                            }
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEvent>();

            var tasks = streamPublisher.Publish(streamProvider);

            streamProvider.PushAsync(new TestEventOne()).FireAndForget();
            streamProvider.PushAsync(new TestEventOne()).FireAndForget();
            streamProvider.PushAsync(new TestEventOne()).FireAndForget();

            await AsyncTestingUtil.WaitAsync(() => received >= 5);

            Func<Task> act = async () => await await Task.WhenAny(tasks);

            await act.Should().ThrowAsync<TargetInvocationException>();
        }

        [Fact]
        public async Task PublishAsync_StreamProvider_ProcessExceptionRethrown()
        {
            var received = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            foreach (var dummy in enumerable)
                            {
                                if (Interlocked.Increment(ref received) >= 3)
                                    throw new TestException();
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            await foreach (var dummy in enumerable)
                            {
                            }
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
        public async Task PublishAsync_StreamProviderAbortedAfterException_AllPendingTasksCompleted()
        {
            var received = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            foreach (var dummy in enumerable)
                            {
                                if (Interlocked.Increment(ref received) >= 3)
                                    throw new TestException();
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            await foreach (var dummy in enumerable)
                            {
                            }
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
        public async Task Publish_StreamProviderOfEnvelopes_StreamedEnvelopesReceived()
        {
            var receivedStreams = 0;
            var receivedEnvelopes = 0;
            var receivedTestEnvelopes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEnvelope> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedEnvelopes);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEnvelope> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEnvelopes);
                            }
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
        public async Task Publish_StreamProviderOfEnvelopes_StreamedUnwrappedMessagesReceived()
        {
            var receivedStreams = 0;
            var receivedTestEventOnes = 0;
            var receivedTestEnvelopes = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEventOnes);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEnvelope> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEnvelopes);
                            }
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
        public async Task Publish_StreamProviderOfEnvelopes_OnlyMatchingUnwrappedMessagesReceived()
        {
            var receivedStreamsOfOnes = 0;
            var receivedTestEventOnes = 0;
            var receivedStreamsOfTwos = 0;
            var receivedTestEventTwos = 0;

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreamsOfOnes);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEventOnes);
                            }
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestEventTwo> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreamsOfTwos);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedTestEventTwos);
                            }
                        }));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var stream = new MessageStreamProvider<IEnvelope>();
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
        public async Task Publish_StreamProviderOfEnvelopes_OnlyAutoUnwrapMessagesReceived()
        {
            var receivedEvents = new List<IEvent>();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            foreach (var envelope in enumerable)
                            {
                                receivedEvents.Add(envelope);
                            }
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

        [Fact]
        public async Task Publish_MessageStreamProvidePlusBehavior_MessagesReceived()
        {
            var receivedStreams = 0;
            var receivedEvents = 0;

            var testBehavior = new TestBehavior();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                            foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref receivedEvents);
                            }
                        })
                    .AddSingletonBehavior(testBehavior));
            var streamPublisher = serviceProvider.GetRequiredService<IStreamPublisher>();

            var streamProvider = new MessageStreamProvider<IEvent>();

            await streamPublisher.PublishAsync(streamProvider);

            await streamProvider.PushAsync(new TestEventOne());
            await streamProvider.PushAsync(new TestEventTwo());

            await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 2);

            receivedStreams.Should().Be(1);
            receivedEvents.Should().Be(2);
        }
    }
}
