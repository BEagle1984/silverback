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
        public async Task Publish_MessageStreamProvider_StreamedMessagesReceived()
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

            var streamProvider1 = new MessageStreamProvider<IEvent>(20);
            var streamProvider2 = new MessageStreamProvider<IEvent>(20);

            Task.Run(() => publisher.Publish(streamProvider1)).RunWithoutBlocking();
            publisher.PublishAsync(streamProvider2).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 4); // TODO: GET RID OF THIS!!
            receivedStreams.Should().Be(4);

            await streamProvider1.PushAsync(new TestEventOne());
            await streamProvider2.PushAsync(new TestEventOne());
            await streamProvider1.PushAsync(new TestEventTwo());
            await streamProvider2.PushAsync(new TestEventTwo());

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
        public async Task Publish_MessageStreamProvider_OnlyRequiredLinkedStreamsPublished()
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

            var streamProvider1 = new MessageStreamProvider<IEvent>(20);
            var streamProvider2 = new MessageStreamProvider<IEvent>(20);

            Task.Run(() => publisher.Publish(streamProvider1)).RunWithoutBlocking();
            publisher.PublishAsync(streamProvider2).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 4);
            receivedStreams.Should().Be(4);
        }

        [Fact]
        public async Task Publish_MessageStreamProvider_ProcessExceptionRethrown()
        {
            var receivedStreams = 0;
            var received = 0;

            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber(
                        (IMessageStreamEnumerable<IEvent> enumerable) =>
                        {
                            Interlocked.Increment(ref receivedStreams);

                            foreach (var dummy in enumerable)
                            {
                                if (Interlocked.Increment(ref received) >= 5)
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

            var streamProvider = new MessageStreamProvider<IEvent>(20);

            var task = publisher.PublishAsync(streamProvider);

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 2); // TODO: GET RID OF THIS!!


            // TODO: PublishAsync must complete only when the streams have been published
            //         -> an additional Task must be returned to catch the error
            // Task streamCompleteTask = await publisher.PublishAsync(streamProvider); -> Task<Task> PublishAsync


            await streamProvider.PushAsync(new TestEventOne());
            await streamProvider.PushAsync(new TestEventOne());
            await streamProvider.PushAsync(new TestEventTwo());
            await streamProvider.PushAsync(new TestEventTwo());

            await AsyncTestingUtil.WaitAsync(() => received >= 5);

            Func<Task> act = async () => await task;
            act.Should().Throw<TargetInvocationException>();
        }

        [Fact]
        public async Task Publish_MessageStreamProviderOfEnvelopes_StreamedEnvelopesReceived()
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

            var streamProvider = new MessageStreamProvider<IEnvelope>(20);
            publisher.PublishAsync(streamProvider).RunWithoutBlocking();

            await AsyncTestingUtil.WaitAsync(() => receivedStreams >= 2);
            receivedStreams.Should().Be(2);

            await streamProvider.PushAsync(new TestEnvelope(new TestEventOne()));
            await streamProvider.PushAsync(new TestEnvelope(new TestEventTwo()));

            await AsyncTestingUtil.WaitAsync(() => receivedEnvelopes >= 2 && receivedTestEnvelopes >= 2);

            receivedEnvelopes.Should().Be(2);
            receivedTestEnvelopes.Should().Be(2);
        }

        [Fact]
        public async Task Publish_MessageStreamProviderOfEnvelopes_StreamedUnwrappedMessagesReceived()
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

            var stream = new MessageStreamProvider<IEnvelope>(20);
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
