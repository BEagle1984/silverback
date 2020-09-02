// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherStreamingTests
    {
        [Fact]
        public async Task Publish_MessageStreamEnumerable_StreamedMessagesReceived()
        {
            var receivedStreams = 0;
            var receivedEvents = 0;
            var receivedTestEventOnes = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
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

            var stream1 = new MessageStreamEnumerable<IEvent>(20);
            var stream2 = new MessageStreamEnumerable<IEvent>(20);

            Task.Run(() => publisher.Publish(stream1)).RunWithoutBlocking();
            publisher.PublishAsync(stream2).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 4);
            receivedStreams.Should().Be(4);

            await stream1.PushAsync(new TestEventOne());
            await stream2.PushAsync(new TestEventOne());
            await stream1.PushAsync(new TestEventTwo());
            await stream2.PushAsync(new TestEventTwo());

            await AsyncTestingUtil.WaitAsync(() => receivedEvents >= 4 && receivedTestEventOnes >= 2);

            receivedEvents.Should().Be(4);
            receivedTestEventOnes.Should().Be(2);
        }

        [Fact]
        public async Task Publish_SimpleMessageAndEnumerableOfMessages_StreamSubscribersNotInvoked()
        {
            var receivedStreams = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
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

            publisher.Publish(new TestEventOne());
            await publisher.PublishAsync(new IEvent[] { new TestEventOne(), new TestEventTwo() });

            receivedStreams.Should().Be(0);
        }

        [Fact]
        public async Task Publish_MessageStreamEnumerable_OnlyRequiredLinkedStreamsPublished()
        {
            var receivedStreams = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                        })
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<TestCommandOne> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);
                        }));

            var stream1 = new MessageStreamEnumerable<IEvent>(20);
            var stream2 = new MessageStreamEnumerable<IEvent>(20);

            Task.Run(() => publisher.Publish(stream1)).RunWithoutBlocking();
            publisher.PublishAsync(stream2).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 4);
            receivedStreams.Should().Be(4);
        }

        [Fact]
        public async Task Publish_MessageStreamEnumerable_ProcessExceptionRethrown()
        {
            var received = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            foreach (var dummy in enumerable)
                            {
                                if (Interlocked.Increment(ref received) >= 5)
                                    throw new TestException();
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                        {
                            await foreach (var dummy in enumerable)
                            {
                                Interlocked.Increment(ref received);
                            }
                        }));

            var stream = new MessageStreamEnumerable<IEvent>(20);

            var task = publisher.PublishAsync(stream);

            await stream.PushAsync(new TestEventOne());
            await stream.PushAsync(new TestEventOne());
            await stream.PushAsync(new TestEventTwo());
            await stream.PushAsync(new TestEventTwo());

            Func<Task> act = async () => await task;
            act.Should().Throw<TargetInvocationException>();
        }

        [Fact]
        public async Task Publish_MessageStreamEnumerableOfEnvelopes_StreamedEnvelopesReceived()
        {
            var receivedStreams = 0;
            var receivedEnvelopes = 0;
            var receivedTestEnvelopes = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
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

            var stream = new MessageStreamEnumerable<IEnvelope>(20);
            publisher.PublishAsync(stream).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 2);
            receivedStreams.Should().Be(2);

            await stream.PushAsync(new TestEnvelope(new TestEventOne()));
            await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

            await AsyncTestingUtil.WaitAsync(() => receivedEnvelopes >= 2 && receivedTestEnvelopes >= 2);

            receivedEnvelopes.Should().Be(2);
            receivedTestEnvelopes.Should().Be(2);
        }

        [Fact]
        public async Task Publish_MessageStreamEnumerableOfEnvelopes_StreamedUnwrappedMessagesReceived()
        {
            var receivedStreams = 0;
            var receivedTestEventOnes = 0;
            var receivedTestEnvelopes = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
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

            var stream = new MessageStreamEnumerable<IEnvelope>(20);
            publisher.PublishAsync(stream).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 2);
            receivedStreams.Should().Be(2);

            await stream.PushAsync(new TestEnvelope(new TestEventOne()));
            await stream.PushAsync(new TestEnvelope(new TestEventTwo()));
            await stream.PushAsync(new TestEnvelope(new TestEventOne()));
            await stream.PushAsync(new TestEnvelope(new TestEventTwo()));

            await AsyncTestingUtil.WaitAsync(() => receivedTestEventOnes >= 2 && receivedTestEnvelopes >= 4);

            receivedTestEventOnes.Should().Be(2);
            receivedTestEnvelopes.Should().Be(4);
        }

        // TODO
        // * Test behaviors?
    }
}
